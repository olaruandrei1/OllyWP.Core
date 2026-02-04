namespace OllyWP.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when a subscription is invalid or malformed
/// </summary>
public class OllyInvalidSubscriptionException : OllyException
{
    /// <summary>
    /// The invalid endpoint (if available)
    /// </summary>
    public string? Endpoint { get; }
    
    public OllyInvalidSubscriptionException() : base() { }
    
    public OllyInvalidSubscriptionException(string message) : base(message) { }
    
    public OllyInvalidSubscriptionException(string message, string endpoint) : base(message)
    {
        Endpoint = endpoint;
    }
    
    public OllyInvalidSubscriptionException(string message, Exception innerException) : base(message, innerException) { }
    
    public OllyInvalidSubscriptionException(string message, string endpoint, Exception innerException) : base(message, innerException)
    {
        Endpoint = endpoint;
    }
}