using System.Text.Json;

namespace OllyWP.Core.Domain.Entities;

/// <summary>
/// Standard Web Push API subscription format
/// This matches the PushSubscription object from browser's service worker
/// </summary>
public class OllySubscription
{
    /// <summary>
    /// Push service endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Subscription keys (P256dh and Auth)
    /// </summary>
    public OllySubscriptionKeys Keys { get; set; } = new();
    
    /// <summary>
    /// Optional: Expiration time (milliseconds since epoch)
    /// Null means no expiration
    /// </summary>
    public long? ExpirationTime { get; set; }
    
    public OllySubscription() { }
    
    public OllySubscription(string endpoint, string p256dh, string auth)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Keys = new OllySubscriptionKeys
        {
            P256dh = p256dh ?? throw new ArgumentNullException(nameof(p256dh)),
            Auth = auth ?? throw new ArgumentNullException(nameof(auth))
        };
    }
    
    /// <summary>
    /// Creates subscription from JSON string (from browser)
    /// </summary>
    public static OllySubscription? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        
        try
        {
            return JsonSerializer.Deserialize<OllySubscription>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Converts subscription to JSON string
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
    
    /// <summary>
    /// Checks if subscription is expired
    /// </summary>
    public bool IsExpired()
    {
        if (!ExpirationTime.HasValue)
            return false;
        
        var expirationDate = DateTimeOffset.FromUnixTimeMilliseconds(ExpirationTime.Value);
        return DateTimeOffset.UtcNow >= expirationDate;
    }
}

/// <summary>
/// Subscription encryption keys
/// </summary>
public class OllySubscriptionKeys
{
    /// <summary>
    /// Client public key (P-256 ECDH, base64url encoded)
    /// Used for encrypting the payload
    /// </summary>
    public string P256dh { get; set; } = string.Empty;
    
    /// <summary>
    /// Client authentication secret (base64url encoded)
    /// Used for authentication during encryption
    /// </summary>
    public string Auth { get; set; } = string.Empty;
}