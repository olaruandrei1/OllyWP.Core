using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Extensions;
using Shouldly;
using Xunit;

namespace OllyWP.Core.UnitTests.Infrastructure;

public class VapidHelperTests
{
    [Fact]
    public void GenerateKeys_ShouldReturnValidKeyPair()
    {
        // Act
        var keys = VapidHelper.GenerateKeys("mailto:test@example.com");

        // Assert
        keys.ShouldNotBeNull();
        keys.PublicKey.ShouldNotBeNullOrEmpty();
        keys.PrivateKey.ShouldNotBeNullOrEmpty();
        keys.Subject.ShouldBe("mailto:test@example.com");
    }

    [Fact]
    public void GenerateKeys_PublicKeyShouldBeBase64Url()
    {
        // Act
        var keys = VapidHelper.GenerateKeys("mailto:test@example.com");

        // Assert
        keys.PublicKey.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_') .ShouldBeTrue();
    }

    [Fact]
    public void GenerateKeys_PrivateKeyShouldBeBase64Url()
    {
        // Act
        var keys = VapidHelper.GenerateKeys("mailto:test@example.com");

        // Assert - base64url only allows: A-Z, a-z, 0-9, -, _
        keys.PrivateKey.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_') .ShouldBeTrue();
    }

    [Fact]
    public void GenerateKeys_CalledTwice_ShouldGenerateDifferentKeys()
    {
        // Act
        var keys1 = VapidHelper.GenerateKeys("mailto:test@example.com");
        var keys2 = VapidHelper.GenerateKeys("mailto:test@example.com");

        // Assert
        keys1.PublicKey.ShouldNotBe(keys2.PublicKey);
        keys1.PrivateKey.ShouldNotBe(keys2.PrivateKey);
    }

    [Theory]
    [InlineData("mailto:admin@example.com")]
    [InlineData("https://example.com")]
    public void GenerateKeys_WithValidSubject_ShouldSetSubject(string subject)
    {
        // Act
        var keys = VapidHelper.GenerateKeys(subject);

        // Assert
        keys.Subject.ShouldBe(subject);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GenerateKeys_WithEmptySubject_ShouldThrowArgumentException(string? subject)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => VapidHelper.GenerateKeys(subject!));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("http://example.com")]
    [InlineData("admin@example.com")]
    public void GenerateKeys_WithInvalidSubjectFormat_ShouldThrowArgumentException(string subject)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => VapidHelper.GenerateKeys(subject));
    }

    [Fact]
    public void ValidateKeys_WithValidKeys_ShouldReturnTrue()
    {
        // Arrange
        var keys = VapidHelper.GenerateKeys("mailto:test@example.com");

        // Act
        var result = VapidHelper.ValidateKeys(keys);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ValidateKeys_WithInvalidKeys_ShouldReturnFalse()
    {
        // Arrange
        var keys = new VapidKeys
        {
            PublicKey = string.Empty,
            PrivateKey = "test",
            Subject = "invalid"
        };

        // Act
        var result = VapidHelper.ValidateKeys(keys);

        // Assert
        result.ShouldBeFalse();
    }
}