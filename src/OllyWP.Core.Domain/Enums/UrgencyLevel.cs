namespace OllyWP.Core.Domain.Enums;

/// <summary>
/// Urgency level for push notification delivery (RFC 8030)
/// </summary>
public enum UrgencyLevel
{
    /// <summary>
    /// Very low priority
    /// </summary>
    VeryLow = 0,
    
    /// <summary>
    /// Low priority
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Normal priority (default)
    /// </summary>
    Normal = 2,
    
    /// <summary>
    /// High priority - deliver immediately
    /// </summary>
    High = 3
}