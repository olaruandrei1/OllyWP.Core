using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace OllyWP.Core.UnitTests.Domain;

public class VapidKeysTests
{
    [Fact]
    public void Validate_WithValidKeys_ShouldNotThrow()
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = "mailto:test@example.com"
        };

        // Act & Assert
        Should.NotThrow(() => keys.Validate());
    }

    [Fact]
    public void Validate_WithEmptyPublicKey_ShouldThrowOllyInvalidKeysException()
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = string.Empty,
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = "mailto:test@example.com"
        };

        // Act & Assert
        var exception = Should.Throw<OllyInvalidKeysException>(() => keys.Validate());
        
        exception.Message.ShouldContain("public key", Case.Insensitive);
    }

    [Fact]
    public void Validate_WithEmptyPrivateKey_ShouldThrowOllyInvalidKeysException()
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = string.Empty,
            Subject = "mailto:test@example.com"
        };

        // Act & Assert
        var exception = Should.Throw<OllyInvalidKeysException>(() => keys.Validate());
        
        exception.Message.ShouldContain("private key", Case.Insensitive);
    }

    [Fact]
    public void Validate_WithEmptySubject_ShouldThrowOllyInvalidKeysException()
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = string.Empty
        };

        // Act & Assert
        var exception = Should.Throw<OllyInvalidKeysException>(() => keys.Validate());
        exception.Message.ShouldContain("subject", Case.Insensitive);
    }

    [Theory]
    [InlineData("mailto:admin@example.com")]
    [InlineData("https://example.com")]
    public void Validate_WithValidSubjectFormats_ShouldNotThrow(string subject)
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = subject
        };

        // Act & Assert
        Should.NotThrow(() => keys.Validate());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("http://example.com")]
    [InlineData("admin@example.com")]
    public void Validate_WithInvalidSubjectFormats_ShouldThrowOllyInvalidKeysException(string subject)
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = subject
        };

        // Act & Assert
        var exception = Should.Throw<OllyInvalidKeysException>(() => keys.Validate());
        exception.Message.ShouldContain("mailto:");
    }

    [Theory]
    [InlineData("BNcRd+XU5/jhaTQ==")]
    [InlineData("BNcRd XU5")]
    public void Validate_WithInvalidBase64UrlPublicKey_ShouldThrowOllyInvalidKeysException(string publicKey)
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = publicKey,
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = "mailto:test@example.com"
        };

        // Act & Assert
        var exception = Should.Throw<OllyInvalidKeysException>(() => keys.Validate());
        exception.Message.ShouldContain("base64url", Case.Insensitive);
    }

    [Fact]
    public void ToJson_ShouldSerializeCorrectly()
    {
        // Arrange
        VapidKeys keys = new()
        {
            PublicKey = "testPublicKey",
            PrivateKey = "testPrivateKey",
            Subject = "mailto:test@example.com"
        };

        // Act
        var json = keys.ToJson();

        // Assert
        json.ShouldContain("testPublicKey");
        json.ShouldContain("testPrivateKey");
        json.ShouldContain("mailto:test@example.com");
    }

    [Fact]
    public void FromJson_WithValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        string json = """
        {
            "publicKey": "testPublicKey",
            "privateKey": "testPrivateKey",
            "subject": "mailto:test@example.com"
        }
        """;

        // Act
        var keys = VapidKeys.FromJson(json);

        // Assert
        keys.ShouldNotBeNull();
        
        keys.PublicKey.ShouldBe("testPublicKey");
        keys.PrivateKey.ShouldBe("testPrivateKey");
        keys.Subject.ShouldBe("mailto:test@example.com");
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrowOllyInvalidKeysException()
    {
        // Arrange
        string json = """
        {
            "publicKey": "",
            "privateKey": "testPrivateKey",
            "subject": "mailto:test@example.com"
        }
        """;

        // Act & Assert
        Should.Throw<OllyInvalidKeysException>(() => VapidKeys.FromJson(json));
    }

    [Fact]
    public void FromJson_WithEmptyString_ShouldReturnNull()
    {
        // Act
        var keys = VapidKeys.FromJson("");

        // Assert
        keys.ShouldBeNull();
    }
}