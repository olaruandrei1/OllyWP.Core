using OllyWP.Core.Domain.Enums;

namespace OllyWP.Core.Domain.HttpResponses;

/// <summary>
/// Result of a push send operation
/// </summary>
public class PushSendResult
{
    public bool Success { get; set; }
    public DeliveryStatus Status { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}