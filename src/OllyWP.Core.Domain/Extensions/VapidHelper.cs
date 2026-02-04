using System.Security.Cryptography;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Exceptions;

namespace OllyWP.Core.Domain.Extensions;

/// <summary>
/// Static helper for VAPID key generation
/// </summary>
public static class VapidHelper
{
    /// <summary>
    /// Generates a new VAPID key pair (P-256 ECDSA)
    /// IMPORTANT: Generate once and store securely! Do not regenerate on every run.
    /// </summary>
    /// <returns>New VAPID key pair</returns>
    public static VapidKeys GenerateKeys()
    {
        try
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(includePrivateParameters: true);
        
            var publicKeyBytes = new byte[65];
            publicKeyBytes[0] = 0x04; 

            Buffer.BlockCopy(parameters.Q.X!, 0, publicKeyBytes, 1, 32);
            Buffer.BlockCopy(parameters.Q.Y!, 0, publicKeyBytes, 33, 32);
        
            var privateKeyBytes = parameters.D!;
        
            return new VapidKeys
            {
                PublicKey = Base64UrlEncode(publicKeyBytes),
                PrivateKey = Base64UrlEncode(privateKeyBytes),
                Subject = string.Empty
            };
        }
        catch (Exception ex)
        {
            throw new OllyCryptoException("Failed to generate VAPID keys.", ex);
        }
    }
    
    /// <summary>
    /// Generates VAPID keys with subject
    /// </summary>
    /// <param name="subject">mailto: or https: URI (e.g., "mailto:admin@example.com")</param>
    public static VapidKeys GenerateKeys(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        
        if (!subject.StartsWith("mailto:") && !subject.StartsWith("https://"))
            throw new ArgumentException("Subject must start with 'mailto:' or 'https://'.", nameof(subject));
        
        var keys = GenerateKeys();
        keys.Subject = subject;
        keys.Validate();
        
        return keys;
    }
    
    /// <summary>
    /// Validates VAPID keys format
    /// </summary>
    public static bool ValidateKeys(VapidKeys keys)
    {
        try
        {
            keys.Validate();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    #region Base64Url Encoding
    
    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
    
    #endregion
}