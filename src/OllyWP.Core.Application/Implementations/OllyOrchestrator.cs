using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using OllyWP.Core.Application.Contracts.Application;
using OllyWP.Core.Application.Contracts.Infrastructure;
using OllyWP.Core.Application.Loggers;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Enums;
using OllyWP.Core.Domain.Exceptions;
using OllyWP.Core.Domain.ValueObjects;

namespace OllyWP.Core.Application.Implementations;

/// <summary>
/// Orchestrates push notification batch processing with parallel execution
/// </summary>
public class OllyOrchestrator : IOllyOrchestrator
{
    private readonly string _subject;
    
    private readonly IVapidService _vapidService;
    private readonly IEncryptionService _encryptionService;
    private readonly IPushSender _pushSender;
    
    private readonly VapidKeys _vapidKeys;
    
    public OllyOrchestrator(
        IVapidService vapidService,
        IEncryptionService encryptionService,
        IPushSender pushSender,
        VapidKeys vapidKeys,
        string subject)
    {
        _vapidService = vapidService ?? throw new ArgumentNullException(nameof(vapidService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _pushSender = pushSender ?? throw new ArgumentNullException(nameof(pushSender));
        _vapidKeys = vapidKeys ?? throw new ArgumentNullException(nameof(vapidKeys));
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        
        _vapidKeys.Validate();
    }
    
    public async Task<OllyResponse> SendBatchesAsync(
        List<OllyBatch> batches,
        int maxDegreeOfParallelism,
        bool continueOnError,
        bool enableLogging,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();
        
        OllyResponse response = new()
        {
            TotalBatches = batches.Count,
            TotalRecipients = batches.Sum(b => b.Recipients.Count)
        };
        
        if (enableLogging)
            ConsoleLogger.Debug($"Orchestrator initialized with {maxDegreeOfParallelism} threads");
        
        try
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };
            
            ConcurrentBag<OllyBatchResult> batchResults = [];
            
            int processedCount = 0;
            
            await Parallel.ForEachAsync(batches, parallelOptions, async (batch, ct) =>
            {
                if (!continueOnError && batchResults.Any(r => !r.Success))
                {
                    if (enableLogging)
                        ConsoleLogger.Warning($"Skipping batch {batch.BatchId} due to previous failure");
                    
                    return;
                }
                
                var batchResult = await ProcessBatchAsync(batch, enableLogging, ct);
                
                batchResults.Add(batchResult);
                
                var count = Interlocked.Increment(ref processedCount);
                
                if (enableLogging)
                {
                    ConsoleLogger.Information($"Completed batch {count}/{batches.Count}: {batchResult.SuccessfulDeliveries}/{batchResult.TotalRecipients} successful");
                }
            });
            
            response.BatchResults = batchResults.OrderBy(b => b.BatchId).ToList();
            response.SuccessfulDeliveries = response.BatchResults.Sum(b => b.SuccessfulDeliveries);
            response.FailedDeliveries = response.BatchResults.Sum(b => b.FailedDeliveries);
            response.Success = response.SuccessfulDeliveries > 0;
        }
        catch (OperationCanceledException)
        {
            response.Success = false;
            response.ErrorMessage = "Operation was cancelled.";
            
            if (enableLogging)
                ConsoleLogger.Warning("Operation cancelled by user");
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = ex.Message;
            
            if (enableLogging)
                ConsoleLogger.Error("Fatal error in orchestrator", ex);
        }
        finally
        {
            stopwatch.Stop();
            response.ElapsedTime = stopwatch.Elapsed;
            
            if (enableLogging)
                ConsoleLogger.Debug($"Total processing time: {response.ElapsedTime.TotalSeconds:F2}s");
        }
        
        return response;
    }
    
    private async Task<OllyBatchResult> ProcessBatchAsync(
        OllyBatch batch,
        bool enableLogging,
        CancellationToken cancellationToken
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new OllyBatchResult
        {
            BatchId = batch.BatchId,
            Payload = batch.Payload,
            TotalRecipients = batch.Recipients.Count
        };
        
        if (enableLogging)
        {
            ConsoleLogger.Information($"Processing batch {batch.BatchId}: {batch.Recipients.Count} recipients");
        }
        
        try
        {
            var payloadJson = SerializePayload(batch.Payload);
            
            if (enableLogging)
                ConsoleLogger.Debug($"Payload size: {payloadJson.Length} bytes");
            
            var deliveryTasks = batch.Recipients.Select((recipient, index) =>
                SendToRecipientAsync(
                    batch.BatchId,
                    recipient,
                    index + 1,
                    batch.Recipients.Count,
                    payloadJson,
                    batch.Payload,
                    enableLogging,
                    cancellationToken));
            
            var deliveryResults = await Task.WhenAll(deliveryTasks);
            
            result.DeliveryResults = deliveryResults.ToList();
            result.SuccessfulDeliveries = deliveryResults.Count(r => r.Success);
            result.FailedDeliveries = deliveryResults.Count(r => !r.Success);
            
            if (enableLogging)
            {
                if (result.Success)
                    ConsoleLogger.Success($"Batch {batch.BatchId}: {result.SuccessfulDeliveries}/{result.TotalRecipients} delivered");
                else
                    ConsoleLogger.Warning($"Batch {batch.BatchId}: {result.FailedDeliveries} failures");
            }
        }
        catch (Exception ex)
        {
            if (enableLogging)
                ConsoleLogger.Error($"Batch {batch.BatchId} failed completely", ex);
            
            result.DeliveryResults = batch.Recipients.Select(r => new OllyDeliveryResult
            {
                BatchId = batch.BatchId,
                Recipient = r,
                Success = false,
                Status = DeliveryStatus.InternalError,
                ErrorMessage = ex.Message,
            }).ToList();
            
            result.FailedDeliveries = result.TotalRecipients;
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    private async Task<OllyDeliveryResult> SendToRecipientAsync(
        Guid batchId,
        OllyRecipient recipient,
        int recipientIndex,
        int totalRecipients,
        string payloadJson,
        OllyPayload payload,
        bool enableLogging,
        CancellationToken cancellationToken
    )
    {
        var stopwatch = Stopwatch.StartNew();
        
        var result = new OllyDeliveryResult
        {
            BatchId = batchId,
            Recipient = recipient,
            AttemptedAt = DateTime.UtcNow
        };
        
        try
        {
            if (enableLogging)
                ConsoleLogger.Debug($"[{recipientIndex}/{totalRecipients}] Processing {recipient.Endpoint[..Math.Min(50, recipient.Endpoint.Length)]}...");
            
            var endpoint = PushEndpoint.FromUrl(recipient.Endpoint);
            
            result.Platform = endpoint.ServiceType;
            
            if (enableLogging)
            {
                ConsoleLogger.Debug($"[{recipientIndex}/{totalRecipients}] Platform: {endpoint.ServiceType}");
            }
            
            if (enableLogging)
            {
                ConsoleLogger.Debug($"[{recipientIndex}/{totalRecipients}] Encrypting...");
            }
            
            var encrypted = await _encryptionService.EncryptAsync(
                recipient.Endpoint,
                recipient.P256dh,
                recipient.Auth,
                payloadJson,
                cancellationToken);
            
            var vapidToken = _vapidService.GenerateToken(
                endpoint.Audience,
                _subject,
                _vapidKeys);
            
            if (enableLogging)
            {
                ConsoleLogger.Debug($"[{recipientIndex}/{totalRecipients}] Sending to {endpoint.ServiceType}...");
            }
            
            var sendResult = await _pushSender.SendAsync(
                recipient.Endpoint,
                encrypted,
                vapidToken,
                _vapidKeys.PublicKey,
                endpoint.ServiceType,
                payload.TimeToLive,
                payload.Urgency,
                cancellationToken);
            
            result.Success = sendResult.Success;
            result.Status = sendResult.Status;
            result.HttpStatusCode = sendResult.HttpStatusCode;
            result.ErrorMessage = sendResult.ErrorMessage;
            
            if (enableLogging)
            {
                if (sendResult.Success)
                {
                    ConsoleLogger.Success($"[{recipientIndex}/{totalRecipients}] Delivered successfully");
                }
                else
                {
                    ConsoleLogger.Error($"[{recipientIndex}/{totalRecipients}] Failed: {sendResult.ErrorMessage}");
                }
            }
        }
        catch (OllyInvalidSubscriptionException ex)
        {
            result.Success = false;
            result.Status = DeliveryStatus.InvalidSubscription;
            result.ErrorMessage = ex.Message;
            
            if (enableLogging)
            {
                ConsoleLogger.Warning($"[{recipientIndex}/{totalRecipients}] Invalid subscription: {ex.Message}");
            }
        }
        catch (OllyCryptoException ex)
        {
            result.Success = false;
            result.Status = DeliveryStatus.EncryptionFailed;
            result.ErrorMessage = ex.Message;
            
            if (enableLogging)
                ConsoleLogger.Error($"[{recipientIndex}/{totalRecipients}] Encryption failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Status = DeliveryStatus.InternalError;
            result.ErrorMessage = ex.Message;
            
            if (enableLogging)
                ConsoleLogger.Error($"[{recipientIndex}/{totalRecipients}] Unexpected error", ex);
        }
        finally
        {
            stopwatch.Stop();
        }
        
        return result;
    }
    
    private static string SerializePayload(OllyPayload payload)
    {
        var payloadObject = new
        {
            title = payload.Title,
            body = payload.Message,
            icon = payload.Icon,
            badge = payload.Badge,
            image = payload.Image,
            url = payload.Url,
            tag = payload.Tag,
            silent = payload.Silent,
            renotify = payload.Renotify,
            data = payload.CustomData
        };
        
        return JsonSerializer.Serialize(payloadObject, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}