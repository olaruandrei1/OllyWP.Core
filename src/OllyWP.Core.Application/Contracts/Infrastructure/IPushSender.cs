using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.HttpResponses;

namespace OllyWP.Core.Application.Contracts.Infrastructure;

/// <summary>
/// Service for sending HTTP requests to push services
/// </summary>
public interface IPushSender
{
    /// <summary>
    /// Sends encrypted push notification to endpoint
    /// </summary>
    Task<PushSendResult> SendAsync(
        string endpoint,
        byte[] encryptedPayload,
        string vapidToken,
        string vapidPublicKey,
        PushServiceType serviceType,
        int? timeToLive,
        UrgencyLevel urgency,
        CancellationToken cancellationToken = default
    );
}