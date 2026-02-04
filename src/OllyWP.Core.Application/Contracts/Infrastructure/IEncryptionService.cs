namespace OllyWP.Core.Application.Contracts.Infrastructure;

/// <summary>
/// Service for encrypting push message payloads (RFC 8291)
/// Implements "aes128gcm" content encoding for Web Push
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a payload for a specific subscription using AES-128-GCM
    /// </summary>
    /// <param name="endpoint">Push endpoint URL</param>
    /// <param name="clientPublicKey">Client's P256dh public key (base64url)</param>
    /// <param name="clientAuthSecret">Client's auth secret (base64url)</param>
    /// <param name="payload">JSON payload to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encrypted payload bytes ready to send</returns>
    Task<byte[]> EncryptAsync(
        string endpoint,
        string clientPublicKey,
        string clientAuthSecret,
        string payload,
        CancellationToken cancellationToken = default
    );
}