using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Engine
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Create, bind and register as service the specified configuration parameters 
        /// </summary>
        /// <typeparam name="TConfig">Type of configuration class.</typeparam>
        /// <param name="configuration">Configuration to bind from.</param>
        public static TConfig ConfigureAppConfig<TConfig>(this IServiceCollection services, IConfiguration configuration)
            where TConfig : class, new()
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(configuration, nameof(configuration));

            // Activate
            var config = new TConfig();

            // Bind to class instance
            configuration.Bind(config);

            // Put as service
            services.AddSingleton(config);

            return config;
        }
    }
}
