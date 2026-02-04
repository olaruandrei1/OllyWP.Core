using System.Net;
using System.Net.Http.Headers;
using OllyWP.Core.Application.Contracts.Infrastructure;
using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.Extensions;
using OllyWP.Core.Domain.HttpResponses;

namespace OllyWP.Core.Infrastructure.Http;

/// <summary>
/// HTTP client for sending push notifications to various push services
/// Implements retry logic and platform-specific handling
/// </summary>
public class PushSender: IPushSender, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;
    
    public PushSender(int maxRetries = 3, int retryDelayMs = 1000)
    {
        _maxRetries = maxRetries;
        _retryDelayMs = retryDelayMs;
        
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
    }
    
    public async Task<PushSendResult> SendAsync(
        string endpoint,
        byte[] encryptedPayload,
        string vapidToken,
        string vapidPublicKey,
        PushServiceType serviceType,
        int? timeToLive,
        UrgencyLevel urgency,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));
        
        if (encryptedPayload == null || encryptedPayload.Length == 0)
            throw new ArgumentException("Encrypted payload cannot be empty.", nameof(encryptedPayload));
        
        var request = BuildRequest(
            endpoint,
            encryptedPayload,
            vapidToken,
            vapidPublicKey,
            serviceType,
            timeToLive,
            urgency
        );
        
        return await SendWithRetryAsync(request, cancellationToken);
    }
    
    #region Request Building
    
    /// <summary>
    /// Builds HTTP request with proper headers for push notification
    /// </summary>
    private HttpRequestMessage BuildRequest(
        string endpoint,
        byte[] encryptedPayload,
        string vapidToken,
        string vapidPublicKey,
        PushServiceType serviceType,
        int? timeToLive,
        UrgencyLevel urgency)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        
        // 1. Content (encrypted payload)
        request.Content = new ByteArrayContent(encryptedPayload);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Content.Headers.ContentEncoding.Add("aes128gcm");
        request.Content.Headers.ContentLength = encryptedPayload.Length;
        
        // 2. VAPID Authorization (RFC 8292)
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"vapid t={vapidToken}, k={vapidPublicKey}");
        
        // 3. TTL (Time-To-Live) - RFC 8030
        var ttl = timeToLive ?? 2419200; // Default: 4 weeks
        request.Headers.TryAddWithoutValidation("TTL", ttl.ToString());
        
        // 4. Urgency - RFC 8030
        request.Headers.TryAddWithoutValidation("Urgency", urgency.ToHeaderValue());
        
        // 5. Platform-specific headers
        AddPlatformSpecificHeaders(request, serviceType);
        
        return request;
    }
    
    /// <summary>
    /// Adds platform-specific headers
    /// </summary>
    private static void AddPlatformSpecificHeaders(HttpRequestMessage request, PushServiceType serviceType)
    {
        switch (serviceType)
        {
            case PushServiceType.ApplePush:
                request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
                request.Headers.TryAddWithoutValidation("apns-priority", "10");
                break;
            case PushServiceType.WindowsWNS:
                request.Headers.TryAddWithoutValidation("X-WNS-Type", "wns/raw");
                request.Headers.TryAddWithoutValidation("X-WNS-RequestForStatus", "true");
                break;
            case PushServiceType.FCM:
            case PushServiceType.MozillaPush:
            case PushServiceType.HuaweiPush:
            case PushServiceType.Generic:
            default:
                break;
        }
    }
    
    #endregion
    
    #region Retry Logic
    
    /// <summary>
    /// Sends request with exponential backoff retry logic
    /// </summary>
    private async Task<PushSendResult> SendWithRetryAsync( HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var requestToSend = attempt == 0 ? request : await CloneRequestAsync(request);
                
                var response = await _httpClient.SendAsync(requestToSend, cancellationToken);
                
                var result = await ParseResponseAsync(response);
                
                if (result.Success || !IsRetryableStatus(result.Status) || attempt >= _maxRetries)
                    return result;

                var delay = _retryDelayMs * (int)Math.Pow(2, attempt);
                
                await Task.Delay(delay, cancellationToken);
                continue;

            }
            catch (TaskCanceledException ex)
            {
                return new PushSendResult
                {
                    Success = false,
                    Status = DeliveryStatus.Timeout,
                    ErrorMessage = "Request timed out."
                };
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                
                if (attempt == _maxRetries)
                {
                    return new PushSendResult
                    {
                        Success = false,
                        Status = DeliveryStatus.NetworkError,
                        ErrorMessage = ex.Message
                    };
                }
                
                var delay = _retryDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }
        
        return new PushSendResult
        {
            Success = false,
            Status = DeliveryStatus.Unknown,
            ErrorMessage = lastException?.Message ?? "Unknown error occurred."
        };
    }
    
    /// <summary>
    /// Checks if status is retryable
    /// </summary>
    private static bool IsRetryableStatus(DeliveryStatus status)
    => status switch
    {
        DeliveryStatus.NetworkError => true,
        DeliveryStatus.Timeout => true,
        DeliveryStatus.ServiceUnavailable => true,
        DeliveryStatus.ServerError => true,
        DeliveryStatus.RateLimited => true,
        _ => false
    };
    
    /// <summary>
    /// Clones HTTP request for retry
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        
        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        
        return clone;
    }
    
    #endregion
    
    #region Response Parsing
    
    /// <summary>
    /// Parses HTTP response into PushSendResult
    /// </summary>
    private async Task<PushSendResult> ParseResponseAsync(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        
        if (response.IsSuccessStatusCode)
        {
            return new()
            {
                Success = true,
                Status = DeliveryStatus.Success,
                HttpStatusCode = statusCode
            };
        }
        
        string? errorBody = null;
        try
        {
            errorBody = await response.Content.ReadAsStringAsync();
        }
        catch { }
        
        var result = new PushSendResult
        {
            Success = false,
            HttpStatusCode = statusCode,
            Status = MapHttpStatusToDeliveryStatus(statusCode),
            ErrorMessage = GetErrorMessage(statusCode, errorBody)
        };
        
        return result;
    }
    
    /// <summary>
    /// Maps HTTP status code to DeliveryStatus
    /// </summary>
    private static DeliveryStatus MapHttpStatusToDeliveryStatus(int statusCode)
    => statusCode switch
    {
        400 => DeliveryStatus.BadRequest,
        401 or 403 => DeliveryStatus.Unauthorized,
        404 or 410 => DeliveryStatus.Expired, 
        413 => DeliveryStatus.PayloadTooLarge,
        429 => DeliveryStatus.RateLimited,
        500 or 502 or 504 => DeliveryStatus.ServerError,
        503 => DeliveryStatus.ServiceUnavailable,
        _ => DeliveryStatus.Unknown
    };
    
    /// <summary>
    /// Gets human-readable error message
    /// </summary>
    private static string GetErrorMessage(int statusCode, string? errorBody)
    {
        var message = statusCode switch
        {
            400 => "Bad request - invalid payload or headers",
            401 => "Unauthorized - invalid VAPID keys",
            403 => "Forbidden - VAPID subject mismatch",
            404 => "Not found - invalid endpoint",
            410 => "Subscription expired - remove from database",
            413 => "Payload too large - reduce payload size",
            429 => "Rate limited - slow down requests",
            500 => "Internal server error - push service issue",
            502 => "Bad gateway - push service unavailable",
            503 => "Service unavailable - try again later",
            504 => "Gateway timeout - push service timeout",
            _ => $"HTTP {statusCode} error"
        };
        
        if (!string.IsNullOrWhiteSpace(errorBody))
            message += $" - {errorBody}";
        
        return message;
    }
    
    #endregion
    
    public void Dispose()
    => _httpClient?.Dispose();
}