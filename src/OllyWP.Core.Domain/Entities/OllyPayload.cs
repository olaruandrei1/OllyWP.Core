using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.Extensions;

namespace OllyWP.Core.Domain.Entities;

/// <summary>
/// Represents the notification content to be sent
/// </summary>
public class OllyPayload
{
    /// <summary>
    /// Notification title (required)
    /// </summary>
    public required string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Notification body/message (required)
    /// </summary>
    public required string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// URL to navigate to when notification is clicked
    /// </summary>
    public required string Url { get; set; }
    
    /// <summary>
    /// Icon displayed in notification (optional)
    /// <para>Direct assignment: Icon = "https://example.com/icon.png"</para>
    /// <para>Or use helpers: WithIconFromFile("path/to/icon.png"), WithIconFromBytes(bytes), WithIconFromBase64(base64)</para>
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Badge icon, usually for app logo (optional)
    /// <para>Direct assignment: Badge = "https://example.com/badge.png"</para>
    /// <para>Or use helpers: WithBadgeFromFile("path/to/badge.png"), WithBadgeFromBytes(bytes), WithBadgeFromBase64(base64)</para>
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// Large image displayed in expanded notification (optional)
    /// <para>Direct assignment: Image = "https://example.com/image.png"</para>
    /// <para>Or use helpers: WithImageFromFile("path/to/image.png"), WithImageFromBytes(bytes), WithImageFromBase64(base64)</para>
    /// </summary>
    public string? Image { get; set; }
    
    /// <summary>
    /// Custom data payload (JSON-serializable)
    /// </summary>
    public Dictionary<string, object>? CustomData { get; set; }
    
    /// <summary>
    /// Time-to-live in seconds (how long push service should attempt delivery)
    /// Default: 4 weeks
    /// </summary>
    public int? TimeToLive { get; set; }
    
    /// <summary>
    /// Urgency level for delivery
    /// </summary>
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Normal;
    
    /// <summary>
    /// Tag to replace previous notifications with same tag
    /// </summary>
    public string? Tag { get; set; }
    
    /// <summary>
    /// Topic for grouping notifications
    /// </summary>
    public string? Topic { get; set; }
    
    /// <summary>
    /// Whether notification should be silent (no sound/vibration)
    /// </summary>
    public bool Silent { get; set; } = false;
    
    /// <summary>
    /// Whether notification should renotify if replaced by same tag
    /// </summary>
    public bool Renotify { get; set; } = false;
    
    public OllyPayload() { }
    
    public OllyPayload(string title, string message)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }
    
    /// <summary>
    /// Validates that required fields are present
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Payload title is required.", nameof(Title));
        
        if (string.IsNullOrWhiteSpace(Message))
            throw new ArgumentException("Payload message is required.", nameof(Message));
        
        if (TimeToLive.HasValue && TimeToLive.Value < 0)
            throw new ArgumentException("TimeToLive must be non-negative.", nameof(TimeToLive));
    }
    
    /// <summary>
    /// Sets icon from file path
    /// </summary>
    public OllyPayload WithIconFromFile(string filePath)
    {
        Icon = ImageHelper.FromFile(filePath);
        return this;
    }
    
    /// <summary>
    /// Sets icon from byte array
    /// </summary>
    public OllyPayload WithIconFromBytes(byte[] bytes, string mimeType = "image/png")
    {
        Icon = ImageHelper.FromBytes(bytes, mimeType);
        return this;
    }
    
    /// <summary>
    /// Sets icon from base64 string
    /// </summary>
    public OllyPayload WithIconFromBase64(string base64, string mimeType = "image/png")
    {
        Icon = ImageHelper.FromBase64(base64, mimeType);
        return this;
    }
    
    /// <summary>
    /// Sets badge from file path
    /// </summary>
    public OllyPayload WithBadgeFromFile(string filePath)
    {
        Badge = ImageHelper.FromFile(filePath);
        return this;
    }
    
    /// <summary>
    /// Sets badge from byte array
    /// </summary>
    public OllyPayload WithBadgeFromBytes(byte[] bytes, string mimeType = "image/png")
    {
        Badge = ImageHelper.FromBytes(bytes, mimeType);
        return this;
    }
    
    /// <summary>
    /// Sets badge from base64 string
    /// </summary>
    public OllyPayload WithBadgeFromBase64(string base64, string mimeType = "image/png")
    {
        Badge = ImageHelper.FromBase64(base64, mimeType);
        return this;
    }
    
    /// <summary>
    /// Sets image from file path
    /// </summary>
    public OllyPayload WithImageFromFile(string filePath)
    {
        Image = ImageHelper.FromFile(filePath);
        return this;
    }
    
    /// <summary>
    /// Sets image from byte array
    /// </summary>
    public OllyPayload WithImageFromBytes(byte[] bytes, string mimeType = "image/png")
    {
        Image = ImageHelper.FromBytes(bytes, mimeType);
        return this;
    }
    
    /// <summary>
    /// Sets image from base64 string
    /// </summary>
    public OllyPayload WithImageFromBase64(string base64, string mimeType = "image/png")
    {
        Image = ImageHelper.FromBase64(base64, mimeType);
        return this;
    }
}
