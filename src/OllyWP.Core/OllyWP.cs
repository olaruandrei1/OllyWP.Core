using Microsoft.Extensions.DependencyInjection;
using OllyWP.Core.Application.Builders;
using OllyWP.Core.Application.Contracts.Application;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Domain.Extensions;

namespace OllyWP.Core;

/// <summary>
/// Static entry point for OllyWP push notifications
/// Self-contained with internal DI - no external DI configuration required
/// </summary>
public static class OllyWp
{
    private static IServiceProvider? _serviceProvider;
    private static readonly Lock _lock = new();
    private static bool _isInitialized = false;
    
    #region Initialization
    
    /// <summary>
    /// Initializes OllyWP with VAPID keys
    /// Call this once at application startup
    /// </summary>
    /// <param name="vapidKeys">VAPID key pair</param>
    /// <param name="maxRetries">Max HTTP retry attempts (default: 3)</param>
    /// <param name="retryDelayMs">Delay between retries in ms (default: 1000)</param>
    public static void Initialize(
        VapidKeys vapidKeys,
        int maxRetries = 3,
        int retryDelayMs = 1000
    )
    {
        ArgumentNullException.ThrowIfNull(vapidKeys);
        
        vapidKeys.Validate();
        
        lock (_lock)
        {
            var services = new ServiceCollection();
            services.AddOllyWp(vapidKeys, maxRetries, retryDelayMs);
            
            _serviceProvider = services.BuildServiceProvider();
            _isInitialized = true;
        }
    }
    
    /// <summary>
    /// Initializes OllyWP by generating new VAPID keys
    /// </summary>
    /// <param name="subject">mailto: or https: URI</param>
    /// <param name="maxRetries">Max HTTP retry attempts (default: 3)</param>
    /// <param name="retryDelayMs">Delay between retries in ms (default: 1000)</param>
    /// <returns>Generated VAPID keys (save these securely!)</returns>
    public static VapidKeys InitializeWithNewKeys(
        string subject,
        int maxRetries = 3,
        int retryDelayMs = 1000
    )
    {
        var vapidKeys = VapidHelper.GenerateKeys(subject);
        
        Initialize(vapidKeys, maxRetries, retryDelayMs);
        
        return vapidKeys;
    }
    
    /// <summary>
    /// Initializes OllyWP with separate key strings
    /// </summary>
    public static void Initialize(
        string publicKey,
        string privateKey,
        string subject,
        int maxRetries = 3,
        int retryDelayMs = 1000
    )
    {
        var vapidKeys = new VapidKeys
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            Subject = subject
        };
        
        Initialize(vapidKeys, maxRetries, retryDelayMs);
    }
    
    /// <summary>
    /// Checks if OllyWP is initialized
    /// </summary>
    public static bool IsInitialized => _isInitialized;
    
    #endregion
    
    #region Fluent API Entry Point
    
    /// <summary>
    /// Starts building a push notification
    /// Example: await OllyWP.DoIt().WithPayload(payload).WithRecipient(recipient).AndSendIt();
    /// </summary>
    public static OllyFluentBuilder DoIt()
    {
        if (!_isInitialized || _serviceProvider == null)
            throw new InvalidOperationException("OllyWP is not initialized. Call OllyWP.Initialize() first.");
        
        return new OllyFluentBuilder(_serviceProvider.GetRequiredService<IOllyOrchestrator>());
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Generates new VAPID keys without initializing
    /// Use this to generate keys, save them, then Initialize() with them later
    /// </summary>
    public static VapidKeys GenerateVapidKeys(string subject)
    => VapidHelper.GenerateKeys(subject);
    
    /// <summary>
    /// Validates VAPID keys without initializing
    /// </summary>
    public static bool ValidateVapidKeys(VapidKeys keys)
    {
        try
        {
            keys.Validate();
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
}