using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Smartstore.Data;

namespace Smartstore.Engine.Initialization
{
    public static class ApplicationInitializerExtensions
    {
        /// <summary>
        /// Registers necessary service for application initialization support.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddApplicationInitializer(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.TryAddTransient<RootApplicationInitializer>();

            return services;
        }

        /// <summary>
        /// Initializes the application by calling all initializers.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <returns>A task that represents the initialization completion.</returns>
        public static async Task InitAsync(this IHost host)
        {
            Guard.NotNull(host, nameof(host));

            if (!DataSettings.DatabaseIsInstalled())
            {
                return;
            }

            using (var scope = host.Services.CreateScope())
            {
                var rootInitializer = scope.ServiceProvider.GetService<RootApplicationInitializer>();
                if (rootInitializer == null)
                {
                    throw new InvalidOperationException("The initialization service is not registered, register it by calling AddApplicationInitializer() on the service collection.");
                }

                await rootInitializer.InitializeAsync();
            }
        }
    }
}