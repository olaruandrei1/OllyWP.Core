namespace OllyWP.Core.Domain.Enums;


/// <summary>
/// Status of a push notification delivery attempt
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Notification was successfully delivered
    /// </summary>
    Success = 0,
    
    /// <summary>
    /// Subscription is invalid or malformed
    /// </summary>
    InvalidSubscription = 1,
    
    /// <summary>
    /// Subscription has expired (HTTP 410 Gone)
    /// </summary>
    Expired = 2,
    
    /// <summary>
    /// Network error occurred during delivery
    /// </summary>
    NetworkError = 3,
    
    /// <summary>
    /// Authentication failed (invalid VAPID keys)
    /// </summary>
    Unauthorized = 4,
    
    /// <summary>
    /// Rate limit exceeded (HTTP 429)
    /// </summary>
    RateLimited = 5,
    
    /// <summary>
    /// Payload size exceeds limit (HTTP 413)
    /// </summary>
    PayloadTooLarge = 6,
    
    /// <summary>
    /// Internal server error from push service (HTTP 500+)
    /// </summary>
    ServerError = 7,
    
    /// <summary>
    /// Bad request (HTTP 400)
    /// </summary>
    BadRequest = 8,
    
    /// <summary>
    /// Service unavailable (HTTP 503)
    /// </summary>
    ServiceUnavailable = 9,
    
    /// <summary>
    /// Timeout occurred during delivery
    /// </summary>
    Timeout = 10,
    
    /// <summary>
    /// Encryption failed
    /// </summary>
    EncryptionFailed = 11,
    
    /// <summary>
    /// Internal error in OllyWP
    /// </summary>
    InternalError = 12,
    
    /// <summary>
    /// Unknown error (HTTP 418)
    /// </summary>
    Unknown = 99
}