using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Data.Caching.Internal;

namespace Smartstore.Data.Caching
{
    public class CachingOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;

        public DbContextOptionsExtensionInfo Info 
            => _info ??= new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAddSingleton<IEfDebugLogger, EfDebugLogger>();
            services.TryAddSingleton<EfCacheKeyGenerator>();
            services.TryAddSingleton<EfCachePolicyResolver>();
            services.TryAddSingleton<EfSqlCommandProcessor>();
            services.TryAddSingleton<EfCacheDependenciesProcessor>();
            services.TryAddSingleton<DbCache>();
            services.TryAddSingleton<EfCacheInterceptor>();
            services.TryAddSingleton<EfCacheInterceptorProcessor>();

            ConfigureOptions(services, null);
        }

        public void Validate(IDbContextOptions options)
        {

        }

        private static void ConfigureOptions(IServiceCollection services, Action<EfCacheOptions> options)
        {
            var cacheOptions = new EfCacheOptions();
            options?.Invoke(cacheOptions);

            services.TryAddSingleton(cacheOptions);
        }

        sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(CachingOptionsExtension extension)
                : base(extension)
            {
            }

            private new CachingOptionsExtension Extension => (CachingOptionsExtension)base.Extension;

            public override long GetServiceProviderHashCode() => 0L;

            public override bool IsDatabaseProvider => true;

            public override string LogFragment => string.Empty;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }

            //public override string LogFragment
            //{
            //    get
            //    {
            //        if (_logFragment == null)
            //        {
            //            var builder = new StringBuilder();

            //            builder.Append(base.LogFragment);

            //            _logFragment = builder.ToString();
            //        }

            //        return _logFragment;
            //    }
            //}
        }
    }
}
