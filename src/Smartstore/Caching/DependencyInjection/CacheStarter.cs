using System;
using Autofac;
using Smartstore.Engine;

namespace Smartstore.Caching.DependencyInjection
{
    public class CacheStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<CacheScopeAccessor>()
                .As<ICacheScopeAccessor>()
                .InstancePerLifetimeScope();

            builder.RegisterType<MemoryCacheStore>()
                .As<ICacheStore>()
                .As<IMemoryCacheStore>()
                .SingleInstance();

            builder.RegisterType<DefaultCacheFactory>()
                .As<ICacheFactory>()
                .SingleInstance();

            builder.RegisterType<HybridCacheManager>()
                .As<ICacheManager>()
                .SingleInstance();
        }
    }
}