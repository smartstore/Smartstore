using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Smartstore.Caching;
using Smartstore.Data;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Redis.Caching;
using Smartstore.Redis.Configuration;
using Smartstore.Redis.Threading;
using Smartstore.Threading;

namespace Smartstore.Redis
{
    public sealed class RedisStarter : StarterBase
    {
        public override int Order => 100;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            // Create and register configuration
            var config = appContext.Configuration;
            var redisConfiguration = new RedisConfiguration();
            config.Bind("Smartstore:Redis", redisConfiguration);
            builder.RegisterInstance(redisConfiguration);

            var hasDefaultConString = redisConfiguration.ConnectionStrings.Default.HasValue();
            var hasCacheConString = redisConfiguration.ConnectionStrings.Cache.HasValue() || hasDefaultConString;
            var hasMessageBusConString = redisConfiguration.ConnectionStrings.Bus.HasValue() || hasDefaultConString;

            builder.RegisterType<RedisConnectionFactory>()
                .As<IRedisConnectionFactory>()
                .SingleInstance();

            builder.RegisterType<RedisJsonSerializer>()
                .As<IRedisSerializer>()
                .SingleInstance();

            if (isActiveModule && hasMessageBusConString)
            {
                builder.Register<RedisMessageBus>(ResolveDefaultMessageBus)
                    .As<IMessageBus>()
                    .AsSelf()
                    .SingleInstance();
            }

            if (isActiveModule && hasCacheConString)
            {
                builder.RegisterType<RedisAsyncState>()
                    .As<IAsyncState>()
                    .AsSelf()
                    .OnPreparing(e =>
                    {
                        // Inject mem based DefaultAsyncState as inner state
                        e.Parameters = new[] { TypedParameter.From<IAsyncState>(new DefaultAsyncState(e.Context.Resolve<IMemoryCacheStore>())) };
                    })
                    .SingleInstance();
            }

            if (isActiveModule && DataSettings.DatabaseIsInstalled() && hasCacheConString)
            {
                builder.RegisterType<RedisCacheStore>()
                    .As<ICacheStore>()
                    .As<IDistributedCacheStore>()
                    .AsSelf()
                    .SingleInstance();
            }
        }

        private static RedisMessageBus ResolveDefaultMessageBus(IComponentContext ctx)
        {
            var connectionFactory = ctx.Resolve<IRedisConnectionFactory>();
            var connectionStrings = ctx.Resolve<RedisConfiguration>().ConnectionStrings;
            var connectionString = connectionStrings.Bus ?? connectionStrings.Default;

            return connectionFactory.GetMessageBus(connectionString);
        }
    }
}