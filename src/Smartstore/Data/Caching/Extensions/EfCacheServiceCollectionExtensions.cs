using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Data.Caching;
using Smartstore.Data.Caching.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EfCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the required services of <see cref="EfCacheInterceptor"/>.
        /// </summary>
        public static IServiceCollection AddEntityFrameworkCache(this IServiceCollection services, Action<EfCacheOptions> options)
        {
            services.TryAddSingleton<IEfDebugLogger, EfDebugLogger>();
            services.TryAddSingleton<EfCacheKeyGenerator>();
            services.TryAddSingleton<EfCachePolicyResolver>();
            services.TryAddSingleton<EfSqlCommandProcessor>();
            services.TryAddSingleton<EfCacheDependenciesProcessor>();
            services.TryAddSingleton<DbCache>();
            services.TryAddSingleton<EfCacheInterceptor>();
            services.TryAddSingleton<EfCacheInterceptorProcessor>();

            ConfigureOptions(services, options);

            return services;
        }

        private static void ConfigureOptions(IServiceCollection services, Action<EfCacheOptions> options)
        {
            var cacheOptions = new EfCacheOptions();
            options?.Invoke(cacheOptions);

            services.TryAddSingleton(cacheOptions);
        }
    }
}
