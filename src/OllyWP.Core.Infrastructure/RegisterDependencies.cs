using Microsoft.Extensions.DependencyInjection;
using OllyWP.Core.Application.Contracts.Infrastructure;
using OllyWP.Core.Infrastructure.Cryptography;
using OllyWP.Core.Infrastructure.Http;

namespace OllyWP.Core.Infrastructure;

/// <summary>
/// Dependency injection registration for Infrastructure layer
/// </summary>
public static class RegisterDependencies
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Infrastructure layer services
        /// </summary>
        public IServiceCollection AddInfrastructureDependencies()
        {
            services.AddSingleton<IVapidService, VapidService>();
            services.AddSingleton<IEncryptionService, MessageEncryption>();
        
            services.AddSingleton<IPushSender>(sp => new PushSender(
                maxRetries: 3,
                retryDelayMs: 1000
            ));
        
            return services;
        }
    }
}