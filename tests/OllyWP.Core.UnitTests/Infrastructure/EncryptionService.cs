using System.Security.Cryptography;
using OllyWP.Core.Domain.Exceptions;
using Shouldly;
using OllyWP.Core.Infrastructure.Cryptography;
using OllyWP.Core.UnitTests.Helpers;
using Xunit;

namespace OllyWP.Core.UnitTests.Infrastructure;

public class EncryptionServiceTests
{
    private readonly MessageEncryption _sut = new MessageEncryption();

    private readonly string _testClientPublicKey = EncryptionTestsHelper.GeneratePublicKey();
    private readonly string _testClientAuthSecret = EncryptionTestsHelper.GenerateAuthKey();

    [Fact]
    public async Task EncryptAsync_WithValidInput_ShouldReturnEncryptedBytes()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var payload = "{\"title\":\"Test\",\"body\":\"Message\"}";

        // Act
        var encrypted = await _sut.EncryptAsync(
            endpoint,
            _testClientPublicKey,
            _testClientAuthSecret,
            payload);

        // Assert
        encrypted.ShouldNotBeNull();
        encrypted.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task EncryptAsync_ShouldProduceCorrectMessageFormat()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var payload = "{\"title\":\"Test\",\"body\":\"Message\"}";

        // Act
        var encrypted = await _sut.EncryptAsync(
            endpoint,
            _testClientPublicKey,
            _testClientAuthSecret,
            payload
        );

        // Assert
        encrypted.Length.ShouldBeGreaterThan(21);

        var salt = encrypted[..16];
        salt.Length.ShouldBe(16);

        var recordSize = (encrypted[16] << 24) | (encrypted[17] << 16) | (encrypted[18] << 8) | encrypted[19];
        recordSize.ShouldBe(4096);

        var keyIdLength = (int)encrypted[20];
        keyIdLength.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task EncryptAsync_CalledTwice_ShouldProduceDifferentOutput()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var clientPublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC";
        var clientAuthSecret = "tBHItJI5svqBDlxcKHAZKw";
        var payload = "{\"title\":\"Test\",\"body\":\"Message\"}";

        // Act
        var encrypted1 = await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, payload);
        var encrypted2 = await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, payload);

        // Assert
        encrypted1.ShouldNotBe(encrypted2);
    }

    [Fact]
    public async Task EncryptAsync_WithDifferentPayloads_ShouldProduceDifferentSizes()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var shortPayload = "{\"title\":\"A\"}";
        var longPayload = "{\"title\":\"" + new string('A', 1000) + "\"}";

        // Act
        var encryptedShort = await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, shortPayload);
        var encryptedLong = await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, longPayload);

        // Assert
        encryptedLong.Length.ShouldBeGreaterThan(encryptedShort.Length);
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EncryptAsync_WithEmptyEndpoint_ShouldThrowArgumentException(string? endpoint)
    {
        // Arrange
        var payload = "{\"title\":\"Test\"}";

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _sut.EncryptAsync(endpoint!, _testClientPublicKey, _testClientAuthSecret, payload));
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EncryptAsync_WithEmptyClientPublicKey_ShouldThrowArgumentException(string? clientPublicKey)
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var payload = "{\"title\":\"Test\"}";

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _sut.EncryptAsync(endpoint, clientPublicKey!, _testClientAuthSecret, payload));
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EncryptAsync_WithEmptyClientAuthSecret_ShouldThrowArgumentException(string? clientAuthSecret)
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var payload = "{\"title\":\"Test\"}";

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _sut.EncryptAsync(endpoint, _testClientPublicKey, clientAuthSecret!, payload));
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EncryptAsync_WithEmptyPayload_ShouldThrowArgumentException(string? payload)
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, payload!));
    }

    [Fact]
    public async Task EncryptAsync_WithInvalidBase64UrlClientPublicKey_ShouldThrowOllyCryptoException()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var clientPublicKey = "invalid+base64/with=padding"; // Invalid base64url
        var payload = "{\"title\":\"Test\"}";

        // Act & Assert
        await Should.ThrowAsync<OllyCryptoException>(async () =>
            await _sut.EncryptAsync(endpoint, clientPublicKey, _testClientAuthSecret, payload));
    }

    [Fact]
    public async Task EncryptAsync_WithInvalidBase64UrlClientAuthSecret_ShouldThrowOllyCryptoException()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var clientAuthSecret = "!!!invalid!!!"; 
        var payload = "{\"title\":\"Test\"}";

        // Act & Assert
        await Should.ThrowAsync<OllyCryptoException>(async () =>
            await _sut.EncryptAsync(endpoint, _testClientPublicKey, clientAuthSecret, payload));
    }
    [Fact]
    public async Task EncryptAsync_WithLargePayload_ShouldEncryptSuccessfully()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";

        var largeData = new string('A', 3000);
        var payload = $"{{\"title\":\"Test\",\"body\":\"{largeData}\"}}";

        // Act
        var encrypted = await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, payload);

        // Assert
        encrypted.ShouldNotBeNull();
        encrypted.Length.ShouldBeGreaterThan(payload.Length); 
    }

    [Fact]
    public async Task EncryptAsync_WithUnicodePayload_ShouldEncryptSuccessfully()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var payload = "{\"title\":\"测试\",\"body\":\"Hello 🔥 Καλημέρα\"}";

        // Act
        var encrypted = await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, payload);

        // Assert
        encrypted.ShouldNotBeNull();
        encrypted.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task EncryptAsync_WithSpecialCharactersPayload_ShouldEncryptSuccessfully()
    {
        // Arrange
        var endpoint = "https://fcm.googleapis.com/fcm/send/test123";
        var payload = "{\"title\":\"Test<>&'\\\"\\n\\r\\t\"}";

        // Act
        var encrypted = await _sut.EncryptAsync(endpoint, _testClientPublicKey, _testClientAuthSecret, payload);

        // Assert
        encrypted.ShouldNotBeNull();
        encrypted.Length.ShouldBeGreaterThan(0);
    }
}