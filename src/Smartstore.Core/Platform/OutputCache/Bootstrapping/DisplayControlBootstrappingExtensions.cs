using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Core.OutputCache;

namespace Smartstore.Core.Bootstrapping
{
    public static class DisplayControlBootstrappingExtensions
    {
        public static IServiceCollection AddDisplayControl(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.TryAddScoped<IDisplayControl, DisplayControl>();
            services.TryAddSingleton(NullOutputCacheInvalidationObserver.Instance);

            return services;
        }
    }
}
