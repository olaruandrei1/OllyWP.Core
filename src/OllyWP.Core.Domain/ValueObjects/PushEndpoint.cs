using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.Exceptions;

namespace OllyWP.Core.Domain.ValueObjects;

/// <summary>
/// Represents a validated push notification endpoint
/// </summary>
public sealed class PushEndpoint
{
    /// <summary>
    /// The full endpoint URL
    /// </summary>
    public string Url { get; }
    
    /// <summary>
    /// The push service type detected from the endpoint
    /// </summary>
    public PushServiceType ServiceType { get; }
    
    /// <summary>
    /// The audience (origin) for VAPID tokens
    /// </summary>
    public string Audience { get; }
    
    /// <summary>
    /// The scheme (http/https)
    /// </summary>
    public string Scheme { get; }
    
    /// <summary>
    /// The host
    /// </summary>
    public string Host { get; }
    
    private PushEndpoint(string url, PushServiceType serviceType, Uri uri)
    {
        Url = url;
        ServiceType = serviceType;
        Audience = $"{uri.Scheme}://{uri.Host}";
        Scheme = uri.Scheme;
        Host = uri.Host;
    }
    
    /// <summary>
    /// Creates a push endpoint from URL string
    /// </summary>
    public static PushEndpoint FromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new OllyInvalidSubscriptionException("Endpoint URL cannot be empty.");
        
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new OllyInvalidSubscriptionException($"Invalid endpoint URL: {url}");
        
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new OllyInvalidSubscriptionException($"Endpoint must use HTTP or HTTPS scheme: {url}");
        
        var serviceType = DetectServiceType(url);
        
        return new PushEndpoint(url, serviceType, uri);
    }
    
    /// <summary>
    /// Detects push service type from endpoint URL
    /// </summary>
    private static PushServiceType DetectServiceType(string endpoint)
    {
        var lower = endpoint.ToLowerInvariant();
    
        // FCM (Firebase Cloud Messaging) - Chrome, Brave, Opera, Edge*, etc.
        // FCM v1 API uses fcm.googleapis.com, legacy GCM uses android.googleapis.com
        if (lower.Contains("fcm.googleapis.com") || lower.Contains("android.googleapis.com"))
            return PushServiceType.FCM;
    
        // Apple Push Notification Service (APNs) - Safari
        // Uses api.push.apple.com (production) and api.sandbox.push.apple.com (development)
        if (lower.Contains("push.apple.com"))
            return PushServiceType.ApplePush;
    
        // Mozilla Push Service - Firefox
        // Uses updates.push.services.mozilla.com for sending notifications
        if (lower.Contains("push.services.mozilla.com"))
            return PushServiceType.MozillaPush;
    
        // Windows Push Notification Service (WNS) - Windows
        // Usually, is returned by Edge
        // Multiple endpoint patterns: notify.windows.com, wns.windows.com, wns2-*.notify.windows.com
        if (lower.Contains("notify.windows.com") || lower.Contains("wns.windows.com"))
            return PushServiceType.WindowsWNS;
    
        // Huawei Push Kit - Huawei devices
        // Uses push-api.cloud.huawei.com or push.hicloud.com
        if (lower.Contains("cloud.huawei.com") || lower.Contains("hicloud.com"))
            return PushServiceType.HuaweiPush;
    
        return PushServiceType.Generic;
    }
    
    public override string ToString() => Url;
    
    public static implicit operator string(PushEndpoint endpoint) => endpoint.Url;
}