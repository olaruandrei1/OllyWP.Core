using OllyWP.Core.Application.Contracts.Application;
using OllyWP.Core.Application.Loggers;
using OllyWP.Core.Domain.Entities;

namespace OllyWP.Core.Application.Builders;

/// <summary>
/// Fluent builder for creating and sending push notification batches
/// </summary>
public class OllyFluentBuilder(IOllyOrchestrator orchestrator)
{
    private readonly IOllyOrchestrator _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    
    private readonly List<OllyBatch> _batches = [];
    
    private OllyPayload? _singlePayload;
    private OllyRecipient? _singleRecipient;
    private List<OllyRecipient>? _singleRecipients;
    
    private int _maxDegreeOfParallelism = 2; 

    private bool _continueOnError = true; 
    private bool _enableLogging = false;

    #region Single Recipient/Payload (Simple Mode)
    
    /// <summary>
    /// Sets a single payload for a single recipient (simple mode)
    /// Use AndSendIt() to send
    /// </summary>
    public OllyFluentBuilder WithPayload(OllyPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        _singlePayload = payload;
        
        return this;
    }
    
    /// <summary>
    /// Sets a single recipient
    /// Use AndSendIt() to send
    /// </summary>
    public OllyFluentBuilder WithRecipient(OllyRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        _singleRecipient = recipient;
        
        return this;
    }
    
    /// <summary>
    /// Sets multiple recipients with same payload (simple mode)
    /// Use AndSendIt() to send
    /// </summary>
    public OllyFluentBuilder WithRecipients(IEnumerable<OllyRecipient> recipients)
    {
        ArgumentNullException.ThrowIfNull(recipients);

        _singleRecipients = recipients.ToList();
        
        return _singleRecipients.Count == 0 ? throw new ArgumentException("At least one recipient is required.", nameof(recipients)) : this;
    }
    
    /// <summary>
    /// Sets multiple recipients with same payload (params)
    /// </summary>
    public OllyFluentBuilder WithRecipients(params OllyRecipient[] recipients)
    => WithRecipients(recipients.AsEnumerable());
    
    #endregion
    
    #region Batch Mode (Advanced)
    
    /// <summary>
    /// Adds a batch with payload and single recipient
    /// Use AndSendItToAll() to send all batches
    /// </summary>
    public OllyFluentBuilder WithBatch(OllyPayload payload, OllyRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(recipient);

        _batches.Add(new OllyBatch
        {
            Payload = payload,
            Recipients = [recipient]
        });
        
        return this;
    }
    
    /// <summary>
    /// Adds a batch with payload and multiple recipients
    /// Use AndSendItToAll() to send all batches
    /// </summary>
    public OllyFluentBuilder WithBatch(OllyPayload payload, IEnumerable<OllyRecipient> recipients)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(recipients);

        var recipientList = recipients.ToList();
        
        if (recipientList.Count == 0)
            throw new ArgumentException("At least one recipient is required.", nameof(recipients));
        
        _batches.Add(new OllyBatch
        {
            Payload = payload,
            Recipients = recipientList
        });
        
        return this;
    }
    
    /// <summary>
    /// Adds a batch with payload and multiple recipients (params)
    /// </summary>
    public OllyFluentBuilder WithBatch(OllyPayload payload, params OllyRecipient[] recipients)
    => WithBatch(payload, recipients.AsEnumerable());
    
    
    /// <summary>
    /// Adds multiple batches at once
    /// </summary>
    public OllyFluentBuilder WithBatches(IEnumerable<OllyBatch> batches)
    {
        ArgumentNullException.ThrowIfNull(batches);
        
        _batches.AddRange(batches);
        
        return this;
    }
    
    #endregion
    
    #region Configuration
    
    /// <summary>
    /// Sets maximum degree of parallelism for batch processing
    /// Default: 2 threads
    /// </summary>
    public OllyFluentBuilder WithMaxParallelism(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1)
            throw new ArgumentException("Max parallelism must be at least 1.", nameof(maxDegreeOfParallelism));
        
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        
        return this;
    }
    
    /// <summary>
    /// Sets whether to continue processing batches if one fails
    /// Default: true (continue on error)
    /// </summary>
    public OllyFluentBuilder WithContinueOnError(bool continueOnError = true)
    {
        _continueOnError = continueOnError;
        
        return this;
    }
    
    /// <summary>
    /// Enables console logging with real-time visual effects
    /// Default: false (no logs)
    /// </summary>
    public OllyFluentBuilder GibMeLogs()
    {
        _enableLogging = true;
        
        ConsoleLogger.Enabled = true;
        
        return this;
    }
    
    #endregion
    
    #region Execution - Simple Mode
    
    /// <summary>
    /// Sends notification to single recipient or multiple recipients with same payload
    /// Use after WithPayload() and WithRecipient(s)
    /// </summary>
    public async Task<OllyResponse> AndSendIt(CancellationToken cancellationToken = default)
    {
        if (_singlePayload == null)
            throw new InvalidOperationException("Payload is required. Call WithPayload() first.");
        
        if (_singleRecipient == null && (_singleRecipients == null || _singleRecipients.Count == 0))
            throw new InvalidOperationException("At least one recipient is required. Call WithRecipient() or WithRecipients() first.");
        
        var recipients = _singleRecipient != null ? [_singleRecipient] : _singleRecipients!;
        
        OllyBatch batch = new()
        {
            Payload = _singlePayload,
            Recipients = recipients
        };
        
        if (_enableLogging)
        {
            ConsoleLogger.Header("OllyWP Push Notification");
            ConsoleLogger.Information($"Sending to {recipients.Count} recipient(s)");
            ConsoleLogger.Separator();
        }
        
        var response = await _orchestrator.SendBatchesAsync(
            [batch],
            _maxDegreeOfParallelism,
            _continueOnError,
            _enableLogging,
            cancellationToken
        );

        if (!_enableLogging) 
            return response;
        
        if (response.Success)
        {
            ConsoleLogger.Success($"Notification sent! {response.SuccessfulDeliveries}/{response.TotalRecipients} delivered");
        }
        else
        {
            ConsoleLogger.Error($"Notification failed: {response.ErrorMessage}");
        }
            
        ConsoleLogger.SingleResult(response);

        return response;
    }
    
    /// <summary>
    /// Sends notification to single recipient or multiple recipients with same payload
    /// Use after WithPayload() and WithRecipient(s)
    /// </summary>
    public Task<OllyResponse> SendIt(CancellationToken cancellationToken = default)
    => AndSendIt(cancellationToken);
    
    #endregion
    
    #region Execution - Batch Mode
    
    /// <summary>
    /// Sends all batches to all recipients
    /// Use after WithBatch()
    /// </summary>
    public async Task<OllyResponse> AndSendItToAll(CancellationToken cancellationToken = default)
    {
        if (_batches.Count == 0)
            throw new InvalidOperationException("No batches to send. Call WithBatch() at least once.");
        
        if (_enableLogging)
        {
            ConsoleLogger.Header("OllyWP Batch Processing");
            
            ConsoleLogger.Information($"Processing {_batches.Count} batches, {_batches.Sum(b => b.Recipients.Count)} total recipients");
            ConsoleLogger.Information($"Max parallelism: {_maxDegreeOfParallelism} threads");
            ConsoleLogger.Information($"Continue on error: {_continueOnError}");
            
            ConsoleLogger.Separator();
        }
        
        var response = await _orchestrator.SendBatchesAsync(
            _batches,
            _maxDegreeOfParallelism,
            _continueOnError,
            _enableLogging,
            cancellationToken);

        if (!_enableLogging) 
            return response;
        
        if (response.Success)
        {
            ConsoleLogger.Success($"Batch processing complete! {response.SuccessfulDeliveries}/{response.TotalRecipients} delivered");
        }
        else
        {
            ConsoleLogger.Error($"Batch processing failed: {response.ErrorMessage}");
        }
            
        ConsoleLogger.BatchResults(response);

        return response;
    }
    
    /// <summary>
    /// Sends all batches to all recipients
    /// Use after WithBatch()
    /// </summary>
    public Task<OllyResponse> SendItToAll(CancellationToken cancellationToken = default)
    => AndSendItToAll(cancellationToken);
    
    #endregion
}