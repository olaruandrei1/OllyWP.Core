using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OllyWP.Core.Application.Contracts.Infrastructure;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Exceptions;

namespace OllyWP.Core.Infrastructure.Cryptography;

/// <summary>
/// VAPID (Voluntary Application Server Identification) service
/// Implements RFC 8292 for Web Push authentication
/// </summary>
public class VapidService : IVapidService
{
    /// <summary>
    /// Generates VAPID JWT token for authentication
    /// Token format: {header}.{claims}.{signature}
    /// </summary>
    public string GenerateToken(
        string audience, 
        string subject, 
        VapidKeys keys, 
        int expirationSeconds = 43200)
    {
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("Audience cannot be empty.", nameof(audience));
        
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));
        
        keys.Validate();
        
        try
        {
            var header = new
            {
                typ = "JWT",
                alg = "ES256"
            };
            
            var expirationTime = DateTimeOffset.UtcNow.AddSeconds(expirationSeconds).ToUnixTimeSeconds();
            
            var claims = new
            {
                aud = audience,
                exp = expirationTime,
                sub = subject
            };
            
            var headerJson = JsonSerializer.Serialize(header);
            var claimsJson = JsonSerializer.Serialize(claims);
            
            var headerEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var claimsEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(claimsJson));
            
            var unsignedToken = $"{headerEncoded}.{claimsEncoded}";
            
            var signature = SignToken(unsignedToken, keys.PrivateKey);
            var signatureEncoded = Base64UrlEncode(signature);
            
            return $"{unsignedToken}.{signatureEncoded}";
        }
        catch (Exception ex)
        {
            throw new OllyCryptoException("Failed to generate VAPID token.", ex);
        }
    }
    
    /// <summary>
    /// Signs the token using ECDSA with P-256 curve and SHA-256
    /// </summary>
    private byte[] SignToken(string data, string privateKeyBase64Url)
    {
        try
        {
            var privateKeyBytes = Base64UrlDecode(privateKeyBase64Url);
        
            if (privateKeyBytes.Length != 32)
                throw new OllyCryptoException($"Invalid private key length: {privateKeyBytes.Length}. Expected 32 bytes for P-256.");
        
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        
            var ecParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = privateKeyBytes
            };
        
            ecdsa.ImportParameters(ecParams);
        
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = ecdsa.SignData(dataBytes, HashAlgorithmName.SHA256);
        
            return signature;
        }
        catch (OllyCryptoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OllyCryptoException("Failed to sign VAPID token.", ex);
        }
    }
    
    #region Base64Url Encoding/Decoding
    
    private static string Base64UrlEncode(byte[] input)
    => Convert.ToBase64String(input)
        .Replace('+', '-')
        .Replace('/', '_')
        .Replace("=", ""); 
    
    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input
            .Replace('-', '+')
            .Replace('_', '/');
        
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        return Convert.FromBase64String(base64);
    }
    
    #endregion
}