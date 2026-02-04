# OllyWP

Modern async-first .NET library for web push notifications with VAPID authentication and AES-128-GCM payload encryption.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download)
[![NuGet](https://img.shields.io/badge/NuGet-coming%20soon-blue.svg)](https://www.nuget.org/)

## Overview

OllyWP is a production-ready library for sending web push notifications in .NET applications. Built from the ground up to support the latest web push standards (RFC 8291, RFC 8292, RFC 8188), it provides a clean, fluent API for sending notifications to all major browser push services.

### Key Features

- **Modern encryption**: Implements aes128gcm content encoding (RFC 8291, RFC 8188) with BouncyCastle for ECDH key agreement
- **VAPID authentication**: Full RFC 8292 implementation with ES256 (ECDSA P-256 + SHA-256)
- **Cross-platform**: Supports all major push services (FCM, Apple Push, Mozilla, Windows WNS, Huawei)
- **Async-first**: Built entirely on async/await patterns for optimal performance
- **Fluent API**: Intuitive builder pattern for constructing and sending notifications
- **Batch processing**: Send to multiple recipients with configurable parallelism
- **Retry logic**: Automatic exponential backoff for transient failures
- **Comprehensive testing**: Extensive unit and integration test coverage

## Installation

```bash
# Coming soon to NuGet
dotnet add package OllyWP.Core
```

## Quick Start

### 1. Initialize OllyWP

Generate VAPID keys once and store them securely:

```csharp
using OllyWP.Core;

// Generate new keys (do this once, then save them!)
var vapidKeys = OllyWp.GenerateVapidKeys("mailto:admin@example.com");

Console.WriteLine($"Public Key: {vapidKeys.PublicKey}");
Console.WriteLine($"Private Key: {vapidKeys.PrivateKey}");
// Store these securely - you'll need them for all future sends
```

Initialize OllyWP at application startup:

```csharp
// Option 1: Initialize with existing keys
OllyWp.Initialize(
    publicKey: "your-public-key",
    privateKey: "your-private-key",
    subject: "mailto:admin@example.com"
);

// Option 2: Initialize with VapidKeys object
var vapidKeys = new VapidKeys
{
    PublicKey = "your-public-key",
    PrivateKey = "your-private-key",
    Subject = "mailto:admin@example.com"
};
OllyWp.Initialize(vapidKeys);
```

### 2. Create a Notification Payload

```csharp
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Enums;

var payload = new OllyPayload
{
    Title = "Hello from OllyWP!",
    Message = "Your notification message here",
    Icon = "https://example.com/icon.png",
    Badge = "https://example.com/badge.png",
    Url = "https://example.com/landing-page",
    Tag = "notification-id",
    Urgency = UrgencyLevel.Normal,
    TimeToLive = 3600 // seconds
};
```

### 3. Send to a Single Recipient

```csharp
using OllyWP.Core.Domain.Entities;

var recipient = new OllyRecipient
{
    Endpoint = "https://fcm.googleapis.com/fcm/send/...",
    P256dh = "client-public-key-base64url",
    Auth = "client-auth-secret-base64url"
};

var response = await OllyWp.DoIt()
    .WithPayload(payload)
    .WithRecipient(recipient)
    .AndSendIt();

if (response.Success)
{
    Console.WriteLine($"Delivered to {response.SuccessfulDeliveries} recipient(s)");
}
```

### 4. Send to Multiple Recipients

```csharp
var recipients = new List<OllyRecipient>
{
    new() { Endpoint = "...", P256dh = "...", Auth = "..." },
    new() { Endpoint = "...", P256dh = "...", Auth = "..." },
    new() { Endpoint = "...", P256dh = "...", Auth = "..." }
};

var response = await OllyWp.DoIt()
    .WithPayload(payload)
    .WithRecipients(recipients)
    .WithMaxParallelism(4) // Send to 4 recipients concurrently
    .AndSendIt();

Console.WriteLine($"Success: {response.SuccessfulDeliveries}/{response.TotalRecipients}");
```

## Advanced Usage

### Batch Processing

Send different notifications to different groups:

```csharp
var batch1 = new OllyPayload { Title = "Group A", Message = "Message for group A" };
var batch2 = new OllyPayload { Title = "Group B", Message = "Message for group B" };

var groupA = new List<OllyRecipient> { /* recipients */ };
var groupB = new List<OllyRecipient> { /* recipients */ };

var response = await OllyWp.DoIt()
    .WithBatch(batch1, groupA)
    .WithBatch(batch2, groupB)
    .WithMaxParallelism(8)
    .WithContinueOnError(true) // Continue even if one batch fails
    .AndSendItToAll();

foreach (var batchResult in response.BatchResults)
{
    Console.WriteLine($"Batch {batchResult.BatchId}: {batchResult.SuccessfulDeliveries} delivered");
}
```

### Error Handling

```csharp
var response = await OllyWp.DoIt()
    .WithPayload(payload)
    .WithRecipient(recipient)
    .AndSendIt();

if (!response.Success)
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
    
    foreach (var batchResult in response.BatchResults)
    {
        foreach (var delivery in batchResult.DeliveryResults)
        {
            if (!delivery.Success)
            {
                Console.WriteLine($"Failed: {delivery.Recipient.Endpoint}");
                Console.WriteLine($"Status: {delivery.Status}");
                Console.WriteLine($"Error: {delivery.ErrorMessage}");
                
                // Handle specific errors
                if (delivery.Status == DeliveryStatus.Expired)
                {
                    // Remove subscription from database
                }
                else if (delivery.Status == DeliveryStatus.RateLimited)
                {
                    // Implement backoff strategy
                }
            }
        }
    }
}
```

### Logging

Enable console logging to see detailed processing information:

```csharp
var response = await OllyWp.DoIt()
    .WithPayload(payload)
    .WithRecipients(recipients)
    .GibMeLogs() // Enable visual console logging
    .AndSendIt();
```

### Custom Configuration

```csharp
// Initialize with custom retry settings
OllyWp.Initialize(
    vapidKeys: vapidKeys,
    maxRetries: 5,        // Retry failed requests up to 5 times
    retryDelayMs: 2000    // Wait 2 seconds between retries (exponential backoff)
);
```

## Technical Details

### Encryption

OllyWP implements the aes128gcm content encoding scheme as specified in RFC 8291 and RFC 8188:

1. **ECDH Key Agreement**: Uses BouncyCastle for P-256 curve operations to compute shared secrets
2. **HKDF**: Derives encryption keys using HMAC-based key derivation (RFC 5869)
3. **AES-128-GCM**: Encrypts payload with authenticated encryption
4. **Message Format**: Constructs properly formatted messages with salt, record size, and server public key

### VAPID Authentication

Implements RFC 8292 Voluntary Application Server Identification:

1. **JWT Generation**: Creates signed tokens with ES256 (ECDSA P-256 + SHA-256)
2. **Token Structure**: Includes audience (push service origin), expiration, and subject (mailto: or https:)
3. **Authorization Header**: Formats tokens as `vapid t={token}, k={publicKey}`

### Platform Support

Works with all major push services:

- **Firebase Cloud Messaging (FCM)**: Google Chrome, Chrome Android, Opera
- **Apple Push Notification Service**: Safari (macOS, iOS)
- **Mozilla Push Service**: Firefox
- **Windows Notification Service (WNS)**: Microsoft Edge
- **Huawei Push Kit**: Huawei devices
- **Generic**: Any RFC 8030 compliant service

## Testing

OllyWP includes comprehensive test coverage:

```bash
# Run all tests
dotnet test
```

Test categories:
- Unit tests for encryption, VAPID, and core logic
- Integration tests for end-to-end notification delivery
- Mock-based tests for HTTP interactions

## Requirements

- .NET 10.0 or higher
- Target frameworks: net10.0

### Dependencies

- BouncyCastle.Cryptography (2.6.2) - ECDH key agreement
- Microsoft.Extensions.DependencyInjection (10.0.2) - Internal DI container

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built on the RFC 8291, RFC 8292, and RFC 8188 specifications
- Uses BouncyCastle for cryptographic operations

## Support

- GitHub Issues: [Report bugs or request features](https://github.com/olaruandrei1/OllyWP/issues)
- Documentation: Coming soon
- NuGet Package: Coming soon

---

**Note**: This library is currently in active development. The API is stable but may receive minor updates before the 1.0.0 release.
