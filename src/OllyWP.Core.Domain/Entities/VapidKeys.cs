using System.Text.Json;
using OllyWP.Core.Domain.Exceptions;

namespace OllyWP.Core.Domain.Entities;

/// <summary>
/// VAPID key pair for server identification (RFC 8292)
/// Generate once and store securely - DO NOT regenerate on every run
/// </summary>
public class VapidKeys
{
    /// <summary>
    /// Public key (base64url encoded, share with clients)
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Private key (base64url encoded, KEEP SECRET!)
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Subject (mailto: or https: URI identifying your server)
    /// Example: "mailto:admin@example.com" or "https://example.com"
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    public VapidKeys() { }
    
    public VapidKeys(string publicKey, string privateKey, string subject)
    {
        PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        PrivateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }
    
    private static bool IsValidBase64Url(string value)
    =>!string.IsNullOrWhiteSpace(value) && value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    
    /// <summary>
    /// Converts to JSON for storage
    /// WARNING: Contains private key - store securely!
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
    
    /// <summary>
    /// Loads from JSON
    /// </summary>
    public static VapidKeys? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
    
        try
        {
            var keys = JsonSerializer.Deserialize<VapidKeys>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        
            if (keys == null) return keys;
            
            if (string.IsNullOrWhiteSpace(keys.PublicKey))
                throw new OllyInvalidKeysException("Public key cannot be empty");
            
            if (string.IsNullOrWhiteSpace(keys.PrivateKey))
                throw new OllyInvalidKeysException("Private key cannot be empty");

            return keys;
        }
        catch (JsonException)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Validates that keys are present and properly formatted
    /// </summary>
    /// <exception cref="OllyInvalidKeysException">Thrown when keys are invalid</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PublicKey))
            throw new OllyInvalidKeysException("VAPID public key is required.");
        
        if (string.IsNullOrWhiteSpace(PrivateKey))
            throw new OllyInvalidKeysException("VAPID private key is required.");
        
        if (string.IsNullOrWhiteSpace(Subject))
            throw new OllyInvalidKeysException("VAPID subject is required.");
        
        if (!Subject.StartsWith("mailto:") && !Subject.StartsWith("https://")) 
            throw new OllyInvalidKeysException("VAPID subject must start with 'mailto:' or 'https://'.");
        
        if (!IsValidBase64Url(PublicKey))
            throw new OllyInvalidKeysException("VAPID public key must be base64url encoded.");
        
        if (!IsValidBase64Url(PrivateKey))
            throw new OllyInvalidKeysException("VAPID private key must be base64url encoded.");
    }
}