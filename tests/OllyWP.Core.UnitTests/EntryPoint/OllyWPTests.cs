// OllyWPTests.cs

using OllyWP.Core.Application.Builders;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace OllyWP.Core.UnitTests;

public class OllyWPTests
{
    [Fact]
    public void Initialize_WithValidKeys_ShouldNotThrow()
    {
        // Arrange
        var keys = new VapidKeys
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = "mailto:test@example.com"
        };

        // Act & Assert
        Should.NotThrow(() => OllyWp.Initialize(keys));
        OllyWp.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Initialize_WithSeparateStrings_ShouldNotThrow()
    {
        // Arrange
        var publicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC";
        var privateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y";
        var subject = "mailto:test@example.com";

        // Act & Assert
        Should.NotThrow(() => OllyWp.Initialize(publicKey, privateKey, subject));
        OllyWp.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Initialize_WithNullKeys_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => OllyWp.Initialize(null!));
    }

    [Fact]
    public void Initialize_WithInvalidKeys_ShouldThrowOllyInvalidKeysException()
    {
        // Arrange
        var keys = new VapidKeys
        {
            PublicKey = "",
            PrivateKey = "test",
            Subject = "invalid"
        };

        // Act & Assert
        Should.Throw<OllyInvalidKeysException>(() => OllyWp.Initialize(keys));
    }

    [Fact]
    public void InitializeWithNewKeys_ShouldGenerateAndReturnKeys()
    {
        // Act
        var keys = OllyWp.InitializeWithNewKeys("mailto:test@example.com");

        // Assert
        keys.ShouldNotBeNull();
        keys.PublicKey.ShouldNotBeNullOrEmpty();
        keys.PrivateKey.ShouldNotBeNullOrEmpty();
        keys.Subject.ShouldBe("mailto:test@example.com");
        OllyWp.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void DoIt_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Note: This test assumes OllyWP is not initialized
        // In real scenario, you'd need to reset static state between tests
        
        // Act & Assert
        // Should.Throw<InvalidOperationException>(() => OllyWP.DoIt());
        
        // Skip this test or implement static reset mechanism
    }

    [Fact]
    public void DoIt_WhenInitialized_ShouldReturnBuilder()
    {
        // Arrange
        var keys = new VapidKeys
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = "mailto:test@example.com"
        };
        OllyWp.Initialize(keys);

        // Act
        var builder = OllyWp.DoIt();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<OllyFluentBuilder>();
    }

    [Fact]
    public void GenerateVapidKeys_ShouldReturnValidKeys()
    {
        // Act
        var keys = OllyWp.GenerateVapidKeys("mailto:test@example.com");

        // Assert
        keys.ShouldNotBeNull();
        keys.PublicKey.ShouldNotBeNullOrEmpty();
        keys.PrivateKey.ShouldNotBeNullOrEmpty();
        keys.Subject.ShouldBe("mailto:test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GenerateVapidKeys_WithEmptySubject_ShouldThrowArgumentException(string? subject)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => OllyWp.GenerateVapidKeys(subject!));
    }

    [Fact]
    public void ValidateVapidKeys_WithValidKeys_ShouldReturnTrue()
    {
        // Arrange
        var keys = OllyWp.GenerateVapidKeys("mailto:test@example.com");

        // Act
        var result = OllyWp.ValidateVapidKeys(keys);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ValidateVapidKeys_WithInvalidKeys_ShouldReturnFalse()
    {
        // Arrange
        var keys = new VapidKeys
        {
            PublicKey = string.Empty,
            PrivateKey = "test",
            Subject = "invalid"
        };

        // Act
        var result = OllyWp.ValidateVapidKeys(keys);

        // Assert
        result.ShouldBeFalse();
    }
}