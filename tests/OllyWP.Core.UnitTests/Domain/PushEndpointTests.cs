using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.Exceptions;
using OllyWP.Core.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace OllyWP.Core.UnitTests.Domain;

public class PushEndpointTests
{
    [Theory]
    [InlineData("https://fcm.googleapis.com/fcm/send/...", PushServiceType.FCM)]
    [InlineData("https://android.googleapis.com/gcm/send/...", PushServiceType.FCM)]
    [InlineData("https://api.push.apple.com/3/device/...", PushServiceType.ApplePush)]
    [InlineData("https://api.sandbox.push.apple.com/3/device/...", PushServiceType.ApplePush)]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v1/...", PushServiceType.MozillaPush)]
    [InlineData("https://wns2-blu001.notify.windows.com/...", PushServiceType.WindowsWNS)]
    [InlineData("https://client.wns.windows.com/...", PushServiceType.WindowsWNS)]
    [InlineData("https://push-api.cloud.huawei.com/v1/...", PushServiceType.HuaweiPush)]
    [InlineData("https://push.hicloud.com/...", PushServiceType.HuaweiPush)]
    [InlineData("https://unknown-service.com/...", PushServiceType.Generic)]
    public void FromUrl_ShouldDetectCorrectPlatform(string url, PushServiceType expected)
    {
        // Act
        var endpoint = PushEndpoint.FromUrl(url);

        // Assert
        endpoint.ServiceType.ShouldBe(expected);
    }

    [Fact]
    public void FromUrl_WithValidUrl_ShouldParseCorrectly()
    {
        // Arrange
        var url = "https://fcm.googleapis.com/fcm/send/abc123";

        // Act
        var endpoint = PushEndpoint.FromUrl(url);

        // Assert
        endpoint.Url.ShouldBe(url);
        endpoint.Scheme.ShouldBe("https");
        endpoint.Host.ShouldBe("fcm.googleapis.com");
        endpoint.Audience.ShouldBe("https://fcm.googleapis.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FromUrl_WithEmptyUrl_ShouldThrowOllyInvalidSubscriptionException(string? url)
    {
        // Act & Assert
        var exception = Should.Throw<OllyInvalidSubscriptionException>(() => PushEndpoint.FromUrl(url!));
        exception.Message.ShouldContain("empty", Case.Insensitive);
    }

    [Fact]
    public void FromUrl_WithInvalidUrl_ShouldThrowOllyInvalidSubscriptionException()
    {
        // Act & Assert
        var exception = Should.Throw<OllyInvalidSubscriptionException>(() => PushEndpoint.FromUrl("not-a-url"));
        exception.Message.ShouldContain("Invalid", Case.Insensitive);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("ws://example.com")]
    public void FromUrl_WithNonHttpScheme_ShouldThrowOllyInvalidSubscriptionException(string url)
    {
        // Act & Assert
        var exception = Should.Throw<OllyInvalidSubscriptionException>(() => PushEndpoint.FromUrl(url));
        exception.Message.ShouldContain("HTTP", Case.Insensitive);
    }

    [Fact]
    public void ToString_ShouldReturnUrl()
    {
        // Arrange
        var url = "https://fcm.googleapis.com/fcm/send/abc123";
        var endpoint = PushEndpoint.FromUrl(url);

        // Act
        var result = endpoint.ToString();

        // Assert
        result.ShouldBe(url);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var url = "https://fcm.googleapis.com/fcm/send/abc123";
        var endpoint = PushEndpoint.FromUrl(url);

        // Act
        string result = endpoint;

        // Assert
        result.ShouldBe(url);
    }
}