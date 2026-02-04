using OllyWP.Core.Domain.Entities;

namespace OllyWP.Core.Application.Contracts.Application;

/// <summary>
/// Orchestrator for push notification batch processing
/// </summary>
public interface IOllyOrchestrator
{
    /// <summary>
    /// Sends batches of push notifications with parallel processing
    /// </summary>
    /// <param name="batches">List of batches to send</param>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent threads</param>
    /// <param name="continueOnError">Whether to continue processing if a batch fails</param>
    /// <param name="enableLogging">Whether to enable real-time console logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overall response with all batch results</returns>
    Task<OllyResponse> SendBatchesAsync(
        List<OllyBatch> batches,
        int maxDegreeOfParallelism,
        bool continueOnError,
        bool enableLogging,
        CancellationToken cancellationToken = default
    );
}