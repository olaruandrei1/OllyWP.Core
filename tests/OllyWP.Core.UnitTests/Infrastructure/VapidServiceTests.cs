using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Extensions;
using OllyWP.Core.Infrastructure.Cryptography;
using Shouldly;
using Xunit;

namespace OllyWP.Core.UnitTests.Infrastructure;

public class VapidServiceTests
{
    private readonly VapidService _sut = new();
    private readonly VapidKeys _validKeys = VapidHelper.GenerateKeys("mailto:test@example.com");

    [Fact]
    public void GenerateToken_WithValidKeys_ShouldReturnJwtToken()
    {
        // Arrange
        var audience = "https://fcm.googleapis.com";
        var subject = "mailto:development@andreiolaru.com";

        // Act
        var token = _sut.GenerateToken(audience, subject, _validKeys);

        // Assert
        token.ShouldNotBeNullOrEmpty();
        token.Split('.').Length.ShouldBe(3);
    }

    [Fact]
    public void GenerateToken_ShouldContainCorrectHeader()
    {
        // Arrange
        var audience = "https://fcm.googleapis.com";
        var subject = "mailto:test@example.com";

        // Act
        var token = _sut.GenerateToken(audience, subject, _validKeys);
        var headerBase64 = token.Split('.')[0];
        var headerJson = Base64UrlDecode(headerBase64);

        // Assert
        headerJson.ShouldContain("\"typ\":\"JWT\"");
        headerJson.ShouldContain("\"alg\":\"ES256\"");
    }

    [Fact]
    public void GenerateToken_ShouldContainCorrectClaims()
    {
        // Arrange
        var audience = "https://fcm.googleapis.com";
        var subject = "mailto:test@example.com";

        // Act
        var token = _sut.GenerateToken(audience, subject, _validKeys);
        var claimsBase64 = token.Split('.')[1];
        var claimsJson = Base64UrlDecode(claimsBase64);

        // Assert
        claimsJson.ShouldContain($"\"aud\":\"{audience}\"");
        claimsJson.ShouldContain($"\"sub\":\"{subject}\"");
        claimsJson.ShouldContain("\"exp\":");
    }

    [Fact]
    public void GenerateToken_WithCustomExpiration_ShouldRespectExpiration()
    {
        // Arrange
        var audience = "https://fcm.googleapis.com";
        var subject = "mailto:test@example.com";
        var expirationSeconds = 3600; // 1 hour

        // Act
        var token = _sut.GenerateToken(audience, subject, _validKeys, expirationSeconds);
        var claimsBase64 = token.Split('.')[1];
        var claimsJson = Base64UrlDecode(claimsBase64);

        var expMatch = System.Text.RegularExpressions.Regex.Match(claimsJson, @"""exp"":(\d+)");
        expMatch.Success.ShouldBeTrue();
        
        var expTimestamp = long.Parse(expMatch.Groups[1].Value);
        var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Assert 
        var difference = expTimestamp - nowTimestamp;
        difference.ShouldBeInRange(expirationSeconds - 60, expirationSeconds + 60);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GenerateToken_WithEmptyAudience_ShouldThrowArgumentException(string? audience)
    {
        // Arrange
        var subject = "mailto:test@example.com";

        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.GenerateToken(audience!, subject, _validKeys));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GenerateToken_WithEmptySubject_ShouldThrowArgumentException(string? subject)
    {
        // Arrange
        var audience = "https://fcm.googleapis.com";

        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.GenerateToken(audience, subject!, _validKeys));
    }

    [Fact]
    public void GenerateToken_WithNullKeys_ShouldThrowArgumentNullException()
    {
        // Arrange
        var audience = "https://fcm.googleapis.com";
        var subject = "mailto:development@andreiolaru.com";

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.GenerateToken(audience, subject, null!));
    }

    [Fact]
    public void GenerateToken_CalledTwice_ShouldGenerateDifferentTokensDueToExpiration()
    {
        // Arrange
        var audience = "https://fcm.googleapis.com";
        var subject = "mailto:development@andreiolaru.com";

        // Act
        var token1 = _sut.GenerateToken(audience, subject, _validKeys);
        Thread.Sleep(1000);
        var token2 = _sut.GenerateToken(audience, subject, _validKeys);

        // Assert
        token1.ShouldNotBe(token2); 
    }

    private static string Base64UrlDecode(string input)
    {
        var base64 = input
            .Replace('-', '+')
            .Replace('_', '/');
        
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        var bytes = Convert.FromBase64String(base64);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}