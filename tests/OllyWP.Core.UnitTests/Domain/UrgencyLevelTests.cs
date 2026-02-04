// Domain/UrgencyLevelTests.cs

using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.Extensions;
using Shouldly;
using Xunit;

namespace OllyWP.Core.UnitTests.Domain;

public class UrgencyLevelTests
{
    [Theory]
    [InlineData(UrgencyLevel.VeryLow, "very-low")]
    [InlineData(UrgencyLevel.Low, "low")]
    [InlineData(UrgencyLevel.Normal, "normal")]
    [InlineData(UrgencyLevel.High, "high")]
    public void ToHeaderValue_ShouldReturnCorrectValue(UrgencyLevel urgency, string expected)
    {
        // Act
        var result = urgency.ToHeaderValue();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("very-low", UrgencyLevel.VeryLow)]
    [InlineData("low", UrgencyLevel.Low)]
    [InlineData("normal", UrgencyLevel.Normal)]
    [InlineData("high", UrgencyLevel.High)]
    [InlineData("NORMAL", UrgencyLevel.Normal)]
    [InlineData("invalid", UrgencyLevel.Normal)]
    public void FromHeaderValue_ShouldParseCorrectly(string value, UrgencyLevel expected)
    {
        // Act
        var result = UrgencyLevelExtensions.FromHeaderValue(value);

        // Assert
        result.ShouldBe(expected);
    }
}
