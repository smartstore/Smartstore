using Smartstore;
using Smartstore.Data.Hooks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HookServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the default hook handler required for database save hooking (without discovering <see cref="IDbSaveHook"/> implementations)
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddDbHookHandler(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.AddSingleton<IDbHookHandler, DefaultDbHookHandler>();
            return services;
        }
    }
}
