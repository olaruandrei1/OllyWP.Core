namespace OllyWP.Core.Domain.Entities;

/// <summary>
/// Represents a single batch: one payload sent to one or more recipients
/// </summary>
public class OllyBatch
{
    /// <summary>
    /// Unique identifier for this batch
    /// </summary>
    public Guid BatchId { get; init; }
    
    /// <summary>
    /// The notification payload to send
    /// </summary>
    public required OllyPayload Payload { get; set; }

    /// <summary>
    /// Recipients who will receive this payload
    /// </summary>
    public required List<OllyRecipient> Recipients { get; set; }
    
    /// <summary>
    /// When this batch was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Optional metadata for tracking
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    public OllyBatch()
    {
        BatchId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Payload = null!;
        Recipients = [];
    }
    
    public OllyBatch(OllyPayload payload, OllyRecipient recipient) : this()
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Recipients = [recipient ?? throw new ArgumentNullException(nameof(recipient))];
    }
    
    public OllyBatch(OllyPayload payload, IEnumerable<OllyRecipient> recipients) : this()
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        
        var recipientList = recipients?.ToList() ?? throw new ArgumentNullException(nameof(recipients));
        
        if (recipientList.Count.Equals(0))
            throw new ArgumentException("At least one recipient is required.", nameof(recipients));
        
        Recipients = recipientList;
    }
}