namespace OllyWP.Core.Domain.Entities;

/// <summary>
/// Represents a push notification recipient with their subscription details
/// </summary>
public class OllyRecipient
{
    /// <summary>
    /// Push service endpoint URL (required)
    /// </summary>
    public required string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Client public key (P-256 ECDH public key, base64url encoded) (required)
    /// </summary>
    public required string P256dh { get; set; } = string.Empty;
    
    /// <summary>
    /// Client authentication secret (base64url encoded) (required)
    /// </summary>
    public string Auth { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Your application's user ID for tracking
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Optional: Device identifier for tracking
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// Optional: Custom metadata for tracking/segmentation
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    public OllyRecipient() { }
    
    public OllyRecipient(string endpoint, string p256dh, string auth)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        P256dh = p256dh ?? throw new ArgumentNullException(nameof(p256dh));
        Auth = auth ?? throw new ArgumentNullException(nameof(auth));
    }
}