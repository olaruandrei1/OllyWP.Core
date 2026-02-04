namespace OllyWP.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when cryptographic operations fail
/// </summary>
public class OllyCryptoException : OllyException
{
    public OllyCryptoException() : base() { }
    
    public OllyCryptoException(string message) : base(message) { }
    
    public OllyCryptoException(string message, Exception innerException) : base(message, innerException) { }
}