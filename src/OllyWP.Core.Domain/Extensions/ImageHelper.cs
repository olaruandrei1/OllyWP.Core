namespace OllyWP.Core.Domain.Extensions;

/// <summary>
/// Helper for converting images to data URIs for push notifications
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Converts file path to data URI
    /// </summary>
    public static string FromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Image file not found: {filePath}");
        
        var bytes = File.ReadAllBytes(filePath);
        var mimeType = GetMimeType(filePath);
        
        return FromBytes(bytes, mimeType);
    }
    
    /// <summary>
    /// Converts byte array to data URI
    /// </summary>
    public static string FromBytes(byte[] bytes, string mimeType = "image/png")
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("Byte array cannot be empty.", nameof(bytes));
        
        var base64 = Convert.ToBase64String(bytes);
        return $"data:{mimeType};base64,{base64}";
    }
    
    /// <summary>
    /// Converts base64 string to data URI
    /// </summary>
    public static string FromBase64(string base64, string mimeType = "image/png")
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException("Base64 string cannot be empty.", nameof(base64));
        
        // Remove data URI prefix if already present
        if (base64.StartsWith("data:"))
            return base64;
        
        // Clean base64 string (remove whitespace, newlines)
        base64 = base64.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        
        return $"data:{mimeType};base64,{base64}";
    }
    
    /// <summary>
    /// Gets MIME type from file extension
    /// </summary>
    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".bmp" => "image/bmp",
            _ => "image/png" // default
        };
    }
    
    /// <summary>
    /// Validates if string is a valid URL
    /// </summary>
    public static bool IsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
    
    /// <summary>
    /// Validates if string is a data URI
    /// </summary>
    public static bool IsDataUri(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.StartsWith("data:");
    }
}