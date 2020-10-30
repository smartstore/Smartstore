using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Data.Caching;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EfCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the required services of <see cref="EfCacheInterceptor"/>.
        /// </summary>
        public static IServiceCollection AddEfCache(this IServiceCollection services, Action<EfCacheOptions> options)
        {
            services.TryAddSingleton<IEfDebugLogger, EfDebugLogger>();
            //services.TryAddSingleton<IReaderWriterLockProvider, ReaderWriterLockProvider>();
            services.TryAddSingleton<IEfCacheKeyProvider, EfCacheKeyProvider>();
            services.TryAddSingleton<IEfCachePolicyParser, EfCachePolicyParser>();
            services.TryAddSingleton<IEfSqlCommandsProcessor, EfSqlCommandsProcessor>();
            services.TryAddSingleton<IEfCacheDependenciesProcessor, EfCacheDependenciesProcessor>();
            services.TryAddSingleton<DbCache>();
            services.TryAddSingleton<EfCacheInterceptor>();

            ConfigureOptions(services, options);

            return services;
        }

        private static void ConfigureOptions(IServiceCollection services, Action<EfCacheOptions> options)
        {
            //var cacheOptions = new EFCoreSecondLevelCacheOptions();
            //options.Invoke(cacheOptions);

            //if (cacheOptions.Settings.CacheProvider == null)
            //{
            //    services.TryAddSingleton<IEFCacheServiceProvider, EFMemoryCacheServiceProvider>();
            //}
            //else
            //{
            //    services.TryAddSingleton(typeof(IEFCacheServiceProvider), cacheOptions.Settings.CacheProvider);
            //}

            //services.TryAddSingleton(Options.Create(cacheOptions.Settings));
        }
    }
}
