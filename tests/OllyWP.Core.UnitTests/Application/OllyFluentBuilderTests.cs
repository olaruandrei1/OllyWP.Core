// Application/OllyFluentBuilderTests.cs
using Shouldly;
using NSubstitute;
using OllyWP.Core.Application.Builders;
using OllyWP.Core.Application.Contracts;
using OllyWP.Core.Application.Contracts.Application;
using OllyWP.Core.Application.Loggers;
using OllyWP.Core.Domain.Entities;
using Xunit;

namespace OllyWP.Core.UnitTests.Application;

public class OllyFluentBuilderTests
{
    private readonly IOllyOrchestrator _orchestratorMock;
    private readonly OllyFluentBuilder _sut;

    public OllyFluentBuilderTests()
    {
        _orchestratorMock = Substitute.For<IOllyOrchestrator>();
        _sut = new OllyFluentBuilder(_orchestratorMock);
    }

    #region Simple Mode (WithPayload + WithRecipient)

    [Fact]
    public void WithPayload_ShouldStorePayload()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test",
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };

        // Act
        var result = _sut.WithPayload(payload);

        // Assert
        result.ShouldBe(_sut); // Returns self for chaining
    }

    [Fact]
    public void WithPayload_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithPayload(null!));
    }

    [Fact]
    public void WithRecipient_ShouldStoreRecipient()
    {
        // Arrange
        var recipient = new OllyRecipient
        {
            Endpoint = "https://fcm.googleapis.com/...",
            P256dh = "test",
            Auth = "test"
        };

        // Act
        var result = _sut.WithRecipient(recipient);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void WithRecipient_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithRecipient(null!));
    }

    [Fact]
    public void WithRecipients_WithList_ShouldStoreRecipients()
    {
        // Arrange
        var recipients = new List<OllyRecipient>
        {
            new() { Endpoint = "https://fcm1.com", P256dh = "key1", Auth = "auth1" },
            new() { Endpoint = "https://fcm2.com", P256dh = "key2", Auth = "auth2" }
        };

        // Act
        var result = _sut.WithRecipients(recipients);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void WithRecipients_WithParams_ShouldStoreRecipients()
    {
        // Arrange
        var recipient1 = new OllyRecipient { Endpoint = "https://fcm1.com", P256dh = "key1", Auth = "auth1" };
        var recipient2 = new OllyRecipient { Endpoint = "https://fcm2.com", P256dh = "key2", Auth = "auth2" };

        // Act
        var result = _sut.WithRecipients(recipient1, recipient2);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void WithRecipients_WithNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithRecipients((IEnumerable<OllyRecipient>)null!));
    }

    [Fact]
    public void WithRecipients_WithEmptyList_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.WithRecipients(new List<OllyRecipient>()));
    }

    [Fact]
    public async Task AndSendIt_WithoutPayload_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var recipient = new OllyRecipient { Endpoint = "test", P256dh = "key", Auth = "auth" };
        _sut.WithRecipient(recipient);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await _sut.AndSendIt());
    }

    [Fact]
    public async Task AndSendIt_WithoutRecipient_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test",
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        _sut.WithPayload(payload);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await _sut.AndSendIt());
    }

    [Fact]
    public async Task AndSendIt_WithPayloadAndRecipient_ShouldCallOrchestrator()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        var recipient = new OllyRecipient { Endpoint = "https://fcm.com", P256dh = "key", Auth = "auth" };
        
        _orchestratorMock
            .SendBatchesAsync(
                Arg.Any<List<OllyBatch>>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new OllyResponse { Success = true });

        // Act
        await _sut
            .WithPayload(payload)
            .WithRecipient(recipient)
            .AndSendIt();

        // Assert
        await _orchestratorMock.Received(1).SendBatchesAsync(
            Arg.Is<List<OllyBatch>>(b => b.Count == 1),
            Arg.Any<int>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Batch Mode (WithBatch)

    [Fact]
    public void WithBatch_WithSingleRecipient_ShouldStoreBatch()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        
        var recipient = new OllyRecipient { Endpoint = "https://fcm.com", P256dh = "key", Auth = "auth" };

        // Act
        var result = _sut.WithBatch(payload, recipient);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void WithBatch_WithMultipleRecipients_ShouldStoreBatch()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        
        var recipients = new List<OllyRecipient>
        {
            new() { Endpoint = "https://fcm1.com", P256dh = "key1", Auth = "auth1" },
            new() { Endpoint = "https://fcm2.com", P256dh = "key2", Auth = "auth2" }
        };

        // Act
        var result = _sut.WithBatch(payload, recipients);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void WithBatch_WithParams_ShouldStoreBatch()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        
        var recipient1 = new OllyRecipient { Endpoint = "https://fcm1.com", P256dh = "key1", Auth = "auth1" };
        var recipient2 = new OllyRecipient { Endpoint = "https://fcm2.com", P256dh = "key2", Auth = "auth2" };

        // Act
        var result = _sut.WithBatch(payload, recipient1, recipient2);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void WithBatch_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        var recipient = new OllyRecipient { Endpoint = "test", P256dh = "key", Auth = "auth" };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithBatch(null!, recipient));
    }

    [Fact]
    public void WithBatch_WithEmptyRecipients_ShouldThrowArgumentException()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.WithBatch(payload, new List<OllyRecipient>()));
    }

    [Fact]
    public void WithBatches_ShouldStoreMultipleBatches()
    {
        // Arrange
        var batches = new List<OllyBatch>
        {
            new() 
            { 
                Payload = new OllyPayload
                {
                    Title = "Test", 
                    Message = "Message",
                    Url = "https://www.andreiolaru.com"
                }, 
                Recipients = []
            },
            new() 
            { 
                Payload = new OllyPayload
                {
                    Title = "Test2", 
                    Message = "Message2",
                    Url = "https://www.andreiolaru.com"
                }, 
                Recipients = []
            },
        };

        // Act
        var result = _sut.WithBatches(batches);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public async Task AndSendItToAll_WithoutBatches_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await _sut.AndSendItToAll());
    }

    [Fact]
    public async Task AndSendItToAll_WithBatches_ShouldCallOrchestrator()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        
        var recipient = new OllyRecipient { Endpoint = "https://fcm.com", P256dh = "key", Auth = "auth" };
        
        _orchestratorMock
            .SendBatchesAsync(
                Arg.Any<List<OllyBatch>>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new OllyResponse { Success = true });

        // Act
        await _sut
            .WithBatch(payload, recipient)
            .AndSendItToAll();

        // Assert
        await _orchestratorMock.Received(1).SendBatchesAsync(
            Arg.Any<List<OllyBatch>>(),
            Arg.Any<int>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Configuration

    [Fact]
    public void WithMaxParallelism_ShouldSetParallelism()
    {
        // Act
        var result = _sut.WithMaxParallelism(10);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void WithMaxParallelism_WithZero_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.WithMaxParallelism(0));
    }

    [Fact]
    public void WithMaxParallelism_WithNegative_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.WithMaxParallelism(-1));
    }

    [Fact]
    public void WithContinueOnError_ShouldSetFlag()
    {
        // Act
        var result = _sut.WithContinueOnError(false);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void GibMeLogs_ShouldEnableLogging()
    {
        // Act
        var result = _sut.GibMeLogs();

        // Assert
        result.ShouldBe(_sut);
        ConsoleLogger.Enabled.ShouldBeTrue();
    }

    #endregion

    #region Method Chaining

    [Fact]
    public async Task FluentAPI_ShouldChainCorrectly()
    {
        // Arrange
        var payload = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        
        var recipient = new OllyRecipient { Endpoint = "https://fcm.com", P256dh = "key", Auth = "auth" };
        
        _orchestratorMock
            .SendBatchesAsync(
                Arg.Any<List<OllyBatch>>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new OllyResponse { Success = true });

        // Act & Assert - Should not throw
        await _sut
            .WithPayload(payload)
            .WithRecipient(recipient)
            .WithMaxParallelism(5)
            .WithContinueOnError(true)
            .GibMeLogs()
            .AndSendIt();
    }

    [Fact]
    public async Task FluentAPI_BatchMode_ShouldChainCorrectly()
    {
        // Arrange
        var payload1 = new OllyPayload
        {
            Title = "Test", 
            Message = "Message",
            Url = "https://www.andreiolaru.com"
        };
        var payload2 = new OllyPayload
        {
            Title = "Test2", 
            Message = "Message2",
            Url = "https://www.andreiolaru.com"
        };
        var recipient1 = new OllyRecipient { Endpoint = "https://fcm1.com", P256dh = "key1", Auth = "auth1" };
        var recipient2 = new OllyRecipient { Endpoint = "https://fcm2.com", P256dh = "key2", Auth = "auth2" };
        
        _orchestratorMock
            .SendBatchesAsync(
                Arg.Any<List<OllyBatch>>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new OllyResponse { Success = true });

        // Act & Assert - Should not throw
        await _sut
            .WithBatch(payload1, recipient1)
            .WithBatch(payload2, recipient2)
            .WithMaxParallelism(10)
            .WithContinueOnError(false)
            .GibMeLogs()
            .AndSendItToAll();
    }

    #endregion
}