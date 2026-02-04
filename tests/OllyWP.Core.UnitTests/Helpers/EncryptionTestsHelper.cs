using System.Security.Cryptography;

namespace OllyWP.Core.UnitTests.Helpers;

public static class EncryptionTestsHelper
{
    public static string GeneratePublicKey()
    {
        using var testEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var testParams = testEcdh.ExportParameters(includePrivateParameters: false);
        
        var publicKeyBytes = new byte[65];
        
        publicKeyBytes[0] = 0x04;
        
        Buffer.BlockCopy(testParams.Q.X!, 0, publicKeyBytes, 1, 32);
        Buffer.BlockCopy(testParams.Q.Y!, 0, publicKeyBytes, 33, 32);
        
        return Convert.ToBase64String(publicKeyBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static string GenerateAuthKey()
    {
        var authBytes = new byte[16];
        
        RandomNumberGenerator.Fill(authBytes);
        
        return Convert.ToBase64String(authBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}