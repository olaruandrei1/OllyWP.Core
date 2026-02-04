using Microsoft.Extensions.DependencyInjection;
using OllyWP.Core.Application.Contracts.Application;
using OllyWP.Core.Application.Implementations;
using OllyWP.Core.Domain.Entities;

namespace OllyWP.Core.Application;

/// <summary>
/// Dependency injection registration for Application layer
/// </summary>
public static class RegisterDependencies
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Application layer services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="vapidKeys">VAPID keys for push notifications</param>
        /// <param name="subject">VAPID subject (mailto: or https:)</param>
        /// <exception cref="ArgumentException"></exception>
        public IServiceCollection AddApplicationDependencies(VapidKeys vapidKeys)
        {
            ArgumentNullException.ThrowIfNull(vapidKeys);

            vapidKeys.Validate();
        
            services.AddSingleton(vapidKeys);
            services.AddSingleton(vapidKeys.Subject); 
        
            services.AddSingleton<IOllyOrchestrator, OllyOrchestrator>();
        
            return services;
        }
    }
}