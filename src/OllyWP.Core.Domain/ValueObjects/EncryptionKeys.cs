using OllyWP.Core.Domain.Exceptions;

namespace OllyWP.Core.Domain.ValueObjects;

public sealed class EncryptionKeys
{
    /// <summary>
    /// Server public key (ephemeral, generated per message)
    /// </summary>
    public required byte[] ServerPublicKey { get; init; }
    
    /// <summary>
    /// Salt used for key derivation
    /// </summary>
    public required byte[] Salt { get; init; }
    
    /// <summary>
    /// Content encryption key (derived)
    /// </summary>
    public required byte[] ContentEncryptionKey { get; init; }
    
    /// <summary>
    /// Nonce for AES-GCM encryption
    /// </summary>
    public required byte[] Nonce { get; init; }
    
    /// <summary>
    /// Shared secret (derived from ECDH)
    /// </summary>
    public required byte[] SharedSecret { get; init; }
    
    private EncryptionKeys() { }
    
    /// <summary>
    /// Creates encryption keys from components
    /// </summary>
    public static EncryptionKeys Create
    (
        byte[] serverPublicKey,
        byte[] salt,
        byte[] contentEncryptionKey,
        byte[] nonce,
        byte[] sharedSecret
    )
    {
        ArgumentNullException.ThrowIfNull(serverPublicKey);
        ArgumentNullException.ThrowIfNull(salt);
        ArgumentNullException.ThrowIfNull(contentEncryptionKey);
        ArgumentNullException.ThrowIfNull(nonce);
        ArgumentNullException.ThrowIfNull(sharedSecret);
        
        return new EncryptionKeys
        {
            ServerPublicKey = serverPublicKey,
            Salt = salt,
            ContentEncryptionKey = contentEncryptionKey,
            Nonce = nonce,
            SharedSecret = sharedSecret
        };
    }
    
    /// <summary>
    /// Validates that all keys have the correct lengths
    /// </summary>
    public void Validate()
    {
        if (Salt.Length != 16)
            throw new OllyCryptoException("Salt must be 16 bytes.");
        
        if (ContentEncryptionKey.Length != 16)
            throw new OllyCryptoException("Content encryption key must be 16 bytes.");
        
        if (Nonce.Length != 12)
            throw new OllyCryptoException("Nonce must be 12 bytes.");
        
        if (ServerPublicKey.Length == 0)
            throw new OllyCryptoException("Server public key cannot be empty.");
        
        if (SharedSecret.Length == 0)
            throw new OllyCryptoException("Shared secret cannot be empty.");
    }
}