using OllyWP.Core.Domain.Entities;

namespace OllyWP.Core.Application.Contracts.Infrastructure;

/// <summary>
/// Service for VAPID authentication (RFC 8292)
/// Voluntary Application Server Identification for Web Push
/// </summary>
public interface IVapidService
{
    /// <summary>
    /// Generates VAPID JWT token for authentication
    /// </summary>
    /// <param name="audience">Push service origin (e.g., https://fcm.googleapis.com)</param>
    /// <param name="subject">mailto: or https: URI identifying your server</param>
    /// <param name="keys">VAPID key pair</param>
    /// <param name="expirationSeconds">Token expiration time (default: 12 hours)</param>
    /// <returns>JWT token for Authorization header</returns>
    string GenerateToken(string audience, string subject, VapidKeys keys, int expirationSeconds = 43200);
}