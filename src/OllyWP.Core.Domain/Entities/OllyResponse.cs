using OllyWP.Core.Domain.Enums;

namespace OllyWP.Core.Domain.Entities;

/// <summary>
/// Response containing results of push notification delivery
/// </summary>
public class OllyResponse
{
    /// <summary>
    /// Overall success (true if at least one delivery succeeded)
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Total number of batches processed
    /// </summary>
    public int TotalBatches { get; set; }
    
    /// <summary>
    /// Total number of recipients across all batches
    /// </summary>
    public int TotalRecipients { get; set; }
    
    /// <summary>
    /// Number of successful deliveries
    /// </summary>
    public int SuccessfulDeliveries { get; set; }
    
    /// <summary>
    /// Number of failed deliveries
    /// </summary>
    public int FailedDeliveries { get; set; }
    
    /// <summary>
    /// Results for each batch
    /// </summary>
    public List<OllyBatchResult> BatchResults { get; set; } = [];
    
    /// <summary>
    /// Total time elapsed for all batches
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }
    
    /// <summary>
    /// Overall error message (if operation failed completely)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets all individual delivery results across all batches
    /// </summary>
    public IEnumerable<OllyDeliveryResult> AllDeliveryResults => BatchResults.SelectMany(b => b.DeliveryResults);
    
    /// <summary>
    /// Gets only failed delivery results
    /// </summary>
    public IEnumerable<OllyDeliveryResult> FailedResults => AllDeliveryResults.Where(r => !r.Success);
    
    /// <summary>
    /// Gets only successful delivery results
    /// </summary>
    public IEnumerable<OllyDeliveryResult> SuccessfulResults => AllDeliveryResults.Where(r => r.Success);
}

/// <summary>
/// Result for a batch
/// </summary>
public class OllyBatchResult
{
    /// <summary>
    /// Unique identifier of this batch
    /// </summary>
    public Guid BatchId { get; set; }
    
    /// <summary>
    /// The payload that was sent in this batch
    /// </summary>
    public OllyPayload Payload { get; set; } = null!;
    
    /// <summary>
    /// Total recipients in this batch
    /// </summary>
    public int TotalRecipients { get; set; }
    
    /// <summary>
    /// Successful deliveries in this batch
    /// </summary>
    public int SuccessfulDeliveries { get; set; }
    
    /// <summary>
    /// Failed deliveries in this batch
    /// </summary>
    public int FailedDeliveries { get; set; }
    
    /// <summary>
    /// Individual delivery results
    /// </summary>
    public List<OllyDeliveryResult> DeliveryResults { get; set; } = [];
    
    /// <summary>
    /// Time taken to process this batch
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
    
    /// <summary>
    /// Batch success (true if at least one delivery succeeded)
    /// </summary>
    public bool Success => SuccessfulDeliveries > 0;
}

/// <summary>
/// Result for a single delivery to one recipient
/// </summary>
public class OllyDeliveryResult
{
    /// <summary>
    /// Batch this delivery belongs to
    /// </summary>
    public Guid BatchId { get; set; }
    
    /// <summary>
    /// The recipient
    /// </summary>
    public OllyRecipient Recipient { get; set; } = null!;
    
    /// <summary>
    /// Whether delivery was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Delivery status
    /// </summary>
    public DeliveryStatus Status { get; set; }
    
    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// HTTP status code from push service
    /// </summary>
    public int? HttpStatusCode { get; set; }
    
    /// <summary>
    /// Push service platform used
    /// </summary>
    public PushServiceType Platform { get; set; }
    
    /// <summary>
    /// Timestamp when delivery was attempted
    /// </summary>
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}