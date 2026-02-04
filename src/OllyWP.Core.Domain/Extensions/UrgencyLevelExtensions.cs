using OllyWP.Core.Domain.Enums;

namespace OllyWP.Core.Domain.Extensions;

/// <summary>
/// Extension methods for UrgencyLevel
/// </summary>
public static class UrgencyLevelExtensions
{
    extension(UrgencyLevel urgencyLevel)
    {
        /// <summary>
        /// Converts UrgencyLevel to RFC 8030 header value
        /// </summary>
        public string ToHeaderValue()  => urgencyLevel switch
        {
            UrgencyLevel.VeryLow => "very-low",
            UrgencyLevel.Low => "low",
            UrgencyLevel.Normal => "normal",
            UrgencyLevel.High => "high",
            _ => "normal"
        };
    }

    extension(string val)
    {
        /// <summary>
        /// Parses RFC 8030 header value to UrgencyLevel
        /// </summary>
        public UrgencyLevel FromHeaderValue() => val?.ToLowerInvariant() switch
        {
            "very-low" => UrgencyLevel.VeryLow,
            "low" => UrgencyLevel.Low,
            "normal" => UrgencyLevel.Normal,
            "high" => UrgencyLevel.High,
            _ => UrgencyLevel.Normal
        };
    }
}