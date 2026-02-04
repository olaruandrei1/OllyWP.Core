namespace OllyWP.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when VAPID keys are invalid or missing
/// </summary>
public class OllyInvalidKeysException : OllyException
{
    public OllyInvalidKeysException() : base() { }
    
    public OllyInvalidKeysException(string message) : base(message) { }
    
    public OllyInvalidKeysException(string message, Exception innerException) 
        : base(message, innerException) { }
}