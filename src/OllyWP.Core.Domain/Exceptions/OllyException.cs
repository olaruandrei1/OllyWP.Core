namespace OllyWP.Core.Domain.Exceptions;

/// <summary>
/// Base exception for all OllyWP exceptions
/// </summary>
public class OllyException : Exception
{
    public OllyException() : base() { }
    
    public OllyException(string message) : base(message) { }
    
    public OllyException(string message, Exception innerException) 
        : base(message, innerException) { }
}