using Microsoft.Extensions.DependencyInjection;
using OllyWP.Core.Domain.Entities;
using OllyWP.Core.Application;
using OllyWP.Core.Infrastructure;

namespace OllyWP.Core;

/// <summary>
/// Dependency injection extensions for OllyWP
/// OPTIONAL: Only use if you want to integrate with your own DI container
/// </summary>
public static class Dependencies
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers all OllyWP services in DI container (advanced usage)
        /// </summary>
        public IServiceCollection AddOllyWp(VapidKeys vapidKeys, int maxRetries = 3, int retryDelayMs = 1000)
        {
            ArgumentNullException.ThrowIfNull(vapidKeys);
        
            vapidKeys.Validate();
        
            services
                .AddInfrastructureDependencies()
                .AddApplicationDependencies(vapidKeys);
        
            return services;
        }
    
        /// <summary>
        /// Registers OllyWP services with separate key strings
        /// </summary>
        public IServiceCollection AddOllyWp( string publicKey, string privateKey, string subject, int maxRetries = 3, int retryDelayMs = 1000)
        {
            var vapidKeys = new VapidKeys
            {
                PublicKey = publicKey,
                PrivateKey = privateKey,
                Subject = subject
            };
        
            return services.AddOllyWp(vapidKeys, maxRetries, retryDelayMs);
        }
    }   
}