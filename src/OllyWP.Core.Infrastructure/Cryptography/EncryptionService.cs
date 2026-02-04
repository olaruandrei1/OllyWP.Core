using System.Security.Cryptography;
using System.Text;
using OllyWP.Core.Application.Contracts.Infrastructure;
using OllyWP.Core.Domain.Exceptions;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace OllyWP.Core.Infrastructure.Cryptography;

/// <summary>
/// Web Push Message Encryption using aes128gcm (RFC 8291 + RFC 8188)
/// Uses BouncyCastle for ECDH to ensure compatibility
/// </summary>
public class MessageEncryption : IEncryptionService
{
    private const int SaltLength = 16;
    private const int AuthSecretLength = 16;
    private const int ContentEncryptionKeyLength = 16;
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private const int RecordSize = 4096;
    
    private static readonly X9ECParameters Curve = ECNamedCurveTable.GetByName("P-256");
    private static readonly ECDomainParameters DomainParams = new(Curve.Curve, Curve.G, Curve.N, Curve.H);
    
    public Task<byte[]> EncryptAsync(string endpoint, string clientPublicKeyBase64Url, string clientAuthSecretBase64Url, string payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientPublicKeyBase64Url);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientAuthSecretBase64Url);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        
        try
        {
            var encryptedPayload = Encrypt(clientPublicKeyBase64Url, clientAuthSecretBase64Url, Encoding.UTF8.GetBytes(payload));
            
            return Task.FromResult(encryptedPayload);
        }
        catch (OllyCryptoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OllyCryptoException("Failed to encrypt push message.", ex);
        }
    }
    
    private static byte[] Encrypt(string clientPublicKeyBase64Url, string clientAuthSecretBase64Url, byte[] plaintext)
    {
        var uaPublic = Base64UrlDecode(clientPublicKeyBase64Url);
        var authSecret = Base64UrlDecode(clientAuthSecretBase64Url);
        
        ValidateClientKeys(uaPublic, authSecret);
        
        var salt = new byte[SaltLength];
        
        RandomNumberGenerator.Fill(salt);
        
        var keyPair = GenerateEcKeyPair();
        var asPublic = GetUncompressedPublicKey(keyPair);
        
        var ecdhSecret = ComputeEcdhSecret(keyPair, uaPublic);
        
        var keyInfo = BuildKeyInfo(uaPublic, asPublic);
        
        var prkKey = HkdfExtract(authSecret, ecdhSecret);
        
        var ikm = HkdfExpand(prkKey, keyInfo, 32);
        
        var prk = HkdfExtract(salt, ikm);
        
        var cekInfo = Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0");
        var cek = HkdfExpand(prk, cekInfo, ContentEncryptionKeyLength);
        
        var nonceInfo = Encoding.ASCII.GetBytes("Content-Encoding: nonce\0");
        var nonce = HkdfExpand(prk, nonceInfo, NonceLength);
        
        var paddedPlaintext = AddPadding(plaintext);
        var ciphertext = EncryptAesGcm(paddedPlaintext, cek, nonce);
        
        return BuildAes128GcmMessage(salt, asPublic, ciphertext);
    }
    
    #region ECDH
    
    private static AsymmetricCipherKeyPair GenerateEcKeyPair()
    {
        var generator = new ECKeyPairGenerator();
        var secureRandom = new SecureRandom();
        var keyGenParams = new ECKeyGenerationParameters(DomainParams, secureRandom);
        
        generator.Init(keyGenParams);
        
        return generator.GenerateKeyPair();
    }
    
    private static byte[] GetUncompressedPublicKey(AsymmetricCipherKeyPair keyPair)
    => ((ECPublicKeyParameters)keyPair.Public).Q.GetEncoded(false);
    
    private static byte[] ComputeEcdhSecret(AsymmetricCipherKeyPair serverKeyPair, byte[] clientPublicKeyBytes)
    {
        var point = Curve.Curve.DecodePoint(clientPublicKeyBytes);
        var clientPublicKey = new ECPublicKeyParameters(point, DomainParams);
        
        var serverPrivateKey = (ECPrivateKeyParameters)serverKeyPair.Private;
        
        var agreement = new ECDHBasicAgreement();
        agreement.Init(serverPrivateKey);
        
        var sharedSecret = agreement.CalculateAgreement(clientPublicKey);
        
        var sharedSecretBytes = sharedSecret.ToByteArrayUnsigned();

        if (sharedSecretBytes.Length >= 32)
            return sharedSecretBytes;
        
        var padded = new byte[32];
        
        Buffer.BlockCopy(sharedSecretBytes, 0, padded, 32 - sharedSecretBytes.Length, sharedSecretBytes.Length);
        
        return padded;
    }
    
    #endregion
    
    #region HKDF (RFC 5869)
    
    private static byte[] HkdfExtract(byte[] salt, byte[] ikm)
    {
        using var hmac = new HMACSHA256(salt);
        
        return hmac.ComputeHash(ikm);
    }
    
    private static byte[] HkdfExpand(byte[] prk, byte[] info, int length)
    {
        using var hmac = new HMACSHA256(prk);
        
        var input = new byte[info.Length + 1];
        
        Buffer.BlockCopy(info, 0, input, 0, info.Length);
        
        input[info.Length] = 0x01;
        
        var output = hmac.ComputeHash(input);

        if (length >= output.Length) 
            return output;
        
        var result = new byte[length];
        
        Buffer.BlockCopy(output, 0, result, 0, length);
        
        return result;
    }
    
    #endregion
    
    #region Key Operations
    
    private static void ValidateClientKeys(byte[] publicKey, byte[] authSecret)
    {
        if (publicKey.Length != 65)
            throw new OllyCryptoException($"Invalid client public key length: {publicKey.Length}. Expected 65 bytes.");
        
        if (publicKey[0] != 0x04)
            throw new OllyCryptoException($"Invalid public key format. Expected 0x04, got 0x{publicKey[0]:X2}.");
        
        if (authSecret.Length != AuthSecretLength)
            throw new OllyCryptoException($"Invalid auth secret length: {authSecret.Length}. Expected {AuthSecretLength} bytes.");
    }
    
    private static byte[] BuildKeyInfo(byte[] uaPublic, byte[] asPublic)
    {
        var prefix = Encoding.ASCII.GetBytes("WebPush: info\0");
        var info = new byte[prefix.Length + uaPublic.Length + asPublic.Length];
        
        Buffer.BlockCopy(prefix, 0, info, 0, prefix.Length);
        Buffer.BlockCopy(uaPublic, 0, info, prefix.Length, uaPublic.Length);
        Buffer.BlockCopy(asPublic, 0, info, prefix.Length + uaPublic.Length, asPublic.Length);
        
        return info;
    }
    
    #endregion
    
    #region Encryption
    
    private static byte[] AddPadding(byte[] plaintext)
    {
        var padded = new byte[plaintext.Length + 1];
        
        Buffer.BlockCopy(plaintext, 0, padded, 0, plaintext.Length);
        padded[plaintext.Length] = 0x02;
        
        return padded;
    }
    
    private static byte[] EncryptAesGcm(byte[] plaintext, byte[] key, byte[] nonce)
    {
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLength];
        
        using var aes = new AesGcm(key, TagLength);
        
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        
        var result = new byte[ciphertext.Length + tag.Length];
        
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
        
        return result;
    }
    
    #endregion
    
    #region Message Building
    
    private static byte[] BuildAes128GcmMessage(byte[] salt, byte[] serverPublicKey, byte[] ciphertext)
    {
        const int headerLength = 86;
        
        var message = new byte[headerLength + ciphertext.Length];
        var offset = 0;
        
        Buffer.BlockCopy(salt, 0, message, offset, SaltLength);
        offset += SaltLength;
        
        message[offset++] = 0x00;
        message[offset++] = 0x00;
        message[offset++] = 0x10;
        message[offset++] = 0x00;
        
        message[offset++] = 65;
        
        Buffer.BlockCopy(serverPublicKey, 0, message, offset, serverPublicKey.Length);
        offset += serverPublicKey.Length;
        
        Buffer.BlockCopy(ciphertext, 0, message, offset, ciphertext.Length);
        
        return message;
    }
    
    #endregion
    
    #region Base64Url
    
    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
    
    #endregion
}