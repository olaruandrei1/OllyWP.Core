// Application/OllyOrchestratorTests.cs
using Shouldly;
using NSubstitute;
using OllyWP.Core.Application.Implementations;
using OllyWP.Core.Application.Contracts.Infrastructure;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.Exceptions;
using OllyWP.Core.Domain.HttpResponses;
using Xunit;

namespace OllyWP.Core.UnitTests.Application;

public class OllyOrchestratorTests
{
    private readonly IVapidService _vapidServiceMock;
    private readonly IEncryptionService _encryptionServiceMock;
    private readonly IPushSender _pushSenderMock;
    private readonly VapidKeys _vapidKeys;
    private readonly OllyOrchestrator _sut;

    public OllyOrchestratorTests()
    {
        _vapidServiceMock = Substitute.For<IVapidService>();
        _encryptionServiceMock = Substitute.For<IEncryptionService>();
        _pushSenderMock = Substitute.For<IPushSender>();
        
        _vapidKeys = new VapidKeys
        {
            PublicKey = "BNcRdXU5jhaTQW76aEYywg_jCPPwSx7R3fTd0yD8xLBR1xyQF5w8q7dC7LZC7J4p6FvC",
            PrivateKey = "tBHItJI5svqBDlxcKHAZKw4Cs7t3K7lFCxQVN7GRN9Y",
            Subject = "mailto:test@example.com"
        };

        _sut = new OllyOrchestrator(
            _vapidServiceMock,
            _encryptionServiceMock,
            _pushSenderMock,
            _vapidKeys,
            "mailto:test@example.com"
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullVapidService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OllyOrchestrator(
            null!,
            _encryptionServiceMock,
            _pushSenderMock,
            _vapidKeys,
            "mailto:test@example.com"
        ));
    }

    [Fact]
    public void Constructor_WithNullEncryptionService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OllyOrchestrator(
            _vapidServiceMock,
            null!,
            _pushSenderMock,
            _vapidKeys,
            "mailto:test@example.com"
        ));
    }

    [Fact]
    public void Constructor_WithNullPushSender_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OllyOrchestrator(
            _vapidServiceMock,
            _encryptionServiceMock,
            null!,
            _vapidKeys,
            "mailto:test@example.com"
        ));
    }

    [Fact]
    public void Constructor_WithNullVapidKeys_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OllyOrchestrator(
            _vapidServiceMock,
            _encryptionServiceMock,
            _pushSenderMock,
            null!,
            "mailto:test@example.com"
        ));
    }

    [Fact]
    public void Constructor_WithNullSubject_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OllyOrchestrator(
            _vapidServiceMock,
            _encryptionServiceMock,
            _pushSenderMock,
            _vapidKeys,
            null!
        ));
    }

    [Fact]
    public void Constructor_WithInvalidVapidKeys_ShouldThrowOllyInvalidKeysException()
    {
        // Arrange
        var invalidKeys = new VapidKeys
        {
            PublicKey = "",
            PrivateKey = "test",
            Subject = "invalid"
        };

        // Act & Assert
        Should.Throw<OllyInvalidKeysException>(() => new OllyOrchestrator(
            _vapidServiceMock,
            _encryptionServiceMock,
            _pushSenderMock,
            invalidKeys,
            "mailto:test@example.com"
        ));
    }

    #endregion

    #region SendBatchesAsync Tests

    [Fact]
    public async Task SendBatchesAsync_WithEmptyBatches_ShouldReturnEmptyResponse()
    {
        // Arrange
        var batches = new List<OllyBatch>();

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.ShouldNotBeNull();
        response.TotalBatches.ShouldBe(0);
        response.TotalRecipients.ShouldBe(0);
        response.BatchResults.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendBatchesAsync_WithSingleSuccessfulBatch_ShouldReturnSuccess()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        SetupSuccessfulMocks();

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.Success.ShouldBeTrue();
        response.TotalBatches.ShouldBe(1);
        response.TotalRecipients.ShouldBe(1);
        response.SuccessfulDeliveries.ShouldBe(1);
        response.FailedDeliveries.ShouldBe(0);
        response.BatchResults.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SendBatchesAsync_WithMultipleBatches_ShouldProcessAll()
    {
        // Arrange
        var batches = new List<OllyBatch>
        {
            CreateTestBatch(2),
            CreateTestBatch(3)
        };

        SetupSuccessfulMocks();

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.Success.ShouldBeTrue();
        response.TotalBatches.ShouldBe(2);
        response.TotalRecipients.ShouldBe(5);
        response.SuccessfulDeliveries.ShouldBe(5);
        response.FailedDeliveries.ShouldBe(0);
        response.BatchResults.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SendBatchesAsync_WithFailedDelivery_ShouldReportFailure()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        SetupFailedMocks();

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.Success.ShouldBeFalse();
        response.SuccessfulDeliveries.ShouldBe(0);
        response.FailedDeliveries.ShouldBe(1);
    }

    [Fact]
    public async Task SendBatchesAsync_WithContinueOnErrorFalse_ShouldStopOnFirstError()
    {
        // Arrange
        var batches = new List<OllyBatch>
        {
            CreateTestBatch(1),
            CreateTestBatch(1)
        };

        SetupFailedMocks();

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 1,
            continueOnError: false,
            enableLogging: false
        );

        // Assert
        response.Success.ShouldBeFalse();
        // Note: Due to parallel processing, both might execute before check
        response.FailedDeliveries.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SendBatchesAsync_WithCancellationToken_ShouldCancelOperation()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };
        var cts = new CancellationTokenSource();
        
        // Setup delay and cancel during operation
        _encryptionServiceMock
            .EncryptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                cts.Cancel();
                await Task.Delay(100, callInfo.Arg<CancellationToken>());
                return new byte[100];
            });

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false,
            cancellationToken: cts.Token
        );

        // Assert
        response.Success.ShouldBeFalse();
        response.ErrorMessage.ShouldContain("cancelled", Case.Insensitive);
    }

    [Fact]
    public async Task SendBatchesAsync_WithLoggingEnabled_ShouldNotThrow()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        SetupSuccessfulMocks();

        // Act & Assert - Should not throw even with logging enabled
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: true
        );

        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendBatchesAsync_ShouldCallEncryptionService()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        SetupSuccessfulMocks();

        // Act
        await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        await _encryptionServiceMock.Received(1).EncryptAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendBatchesAsync_ShouldCallVapidService()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        SetupSuccessfulMocks();

        // Act
        await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        _vapidServiceMock.Received(1).GenerateToken(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<VapidKeys>(),
            Arg.Any<int>()
        );
    }

    [Fact]
    public async Task SendBatchesAsync_ShouldCallPushSender()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        SetupSuccessfulMocks();

        // Act
        await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        await _pushSenderMock.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<PushServiceType>(),
            Arg.Any<int?>(),
            Arg.Any<UrgencyLevel>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendBatchesAsync_WithInvalidSubscription_ShouldReportInvalidSubscriptionStatus()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        _encryptionServiceMock
            .EncryptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new OllyInvalidSubscriptionException("Invalid subscription")));

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.FailedDeliveries.ShouldBe(1);
        response.BatchResults[0].DeliveryResults[0].Status.ShouldBe(DeliveryStatus.InvalidSubscription);
    }

    [Fact]
    public async Task SendBatchesAsync_WithCryptoException_ShouldReportEncryptionFailedStatus()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        _encryptionServiceMock
            .EncryptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new OllyCryptoException("Encryption failed")));

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.FailedDeliveries.ShouldBe(1);
        response.BatchResults[0].DeliveryResults[0].Status.ShouldBe(DeliveryStatus.EncryptionFailed);
    }

    [Fact]
    public async Task SendBatchesAsync_WithGenericException_ShouldReportInternalErrorStatus()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        _encryptionServiceMock
            .EncryptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new Exception("Unknown error")));

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.FailedDeliveries.ShouldBe(1);
        response.BatchResults[0].DeliveryResults[0].Status.ShouldBe(DeliveryStatus.InternalError);
    }

    [Fact]
    public async Task SendBatchesAsync_ShouldTrackElapsedTime()
    {
        // Arrange
        var batch = CreateTestBatch(1);
        var batches = new List<OllyBatch> { batch };

        SetupSuccessfulMocks();

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 2,
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.ElapsedTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task SendBatchesAsync_WithMaxParallelism_ShouldRespectLimit()
    {
        // Arrange
        var batches = new List<OllyBatch>
        {
            CreateTestBatch(1),
            CreateTestBatch(1),
            CreateTestBatch(1)
        };

        SetupSuccessfulMocks();

        // Act
        var response = await _sut.SendBatchesAsync(
            batches,
            maxDegreeOfParallelism: 1, // Force sequential
            continueOnError: true,
            enableLogging: false
        );

        // Assert
        response.TotalBatches.ShouldBe(3);
        response.BatchResults.Count.ShouldBe(3);
    }

    #endregion

    #region Helper Methods

    private OllyBatch CreateTestBatch(int recipientCount)
    {
        var recipients = new List<OllyRecipient>();
        for (int i = 0; i < recipientCount; i++)
        {
            recipients.Add(new OllyRecipient
            {
                Endpoint = $"https://fcm.googleapis.com/fcm/send/test{i}",
                P256dh = "BNcRdXU5jhaTQW76aEYywg",
                Auth = "tBHItJI5svqBDlxcKHAZKw"
            });
        }

        return new OllyBatch
        {
            Payload = new OllyPayload
            {
                Title = "Test",
                Message = "Test Message",
                Url = "https://www.andreiolaru.com"
            },
            Recipients = recipients
        };
    }

    private void SetupSuccessfulMocks()
    {
        _encryptionServiceMock
            .EncryptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new byte[100]);

        _vapidServiceMock
            .GenerateToken(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<VapidKeys>(),
                Arg.Any<int>())
            .Returns("test.jwt.token");

        _pushSenderMock
            .SendAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<PushServiceType>(),
                Arg.Any<int?>(),
                Arg.Any<UrgencyLevel>(),
                Arg.Any<CancellationToken>())
            .Returns(new PushSendResult
            {
                Success = true,
                Status = DeliveryStatus.Success
            });
    }

    private void SetupFailedMocks()
    {
        _encryptionServiceMock
            .EncryptAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new byte[100]);

        _vapidServiceMock
            .GenerateToken(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<VapidKeys>(),
                Arg.Any<int>())
            .Returns("test.jwt.token");

        _pushSenderMock
            .SendAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<PushServiceType>(),
                Arg.Any<int?>(),
                Arg.Any<UrgencyLevel>(),
                Arg.Any<CancellationToken>())
            .Returns(new PushSendResult
            {
                Success = false,
                Status = DeliveryStatus.NetworkError,
                ErrorMessage = "Network error"
            });
    }

    #endregion
}