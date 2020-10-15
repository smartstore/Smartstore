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

namespace Smartstore.Redis
{
    public sealed class RedisStarter : StarterBase
    {
        public override int Order => 100;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            var config = appContext.Configuration;

            var hasDefaultConString = config.GetConnectionString("Smartstore.Redis").HasValue();
            var hasCacheConString = config.GetConnectionString("Smartstore.Redis.Cache").HasValue() || hasDefaultConString;
            var hasMessageBusConString = config.GetConnectionString("Smartstore.Redis.MessageBus").HasValue() || hasDefaultConString;

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

            //if (isActiveModule && hasCacheConString)
            //{
            //    builder.RegisterType<RedisAsyncState>().As<IAsyncState>().SingleInstance();
            //}

            if (isActiveModule && DataSettings.DatabaseIsInstalled() && hasCacheConString)
            {
                builder.RegisterType<RedisCacheStore>()
                    .As<ICacheStore>()
                    .As<IDistributedCacheStore>()
                    .SingleInstance();
            }
        }

        private static RedisMessageBus ResolveDefaultMessageBus(IComponentContext ctx)
        {
            var connectionFactory = ctx.Resolve<IRedisConnectionFactory>();
            var connectionString = connectionFactory.GetConnectionString("Smartstore.Redis.MessageBus");

            return connectionFactory.GetMessageBus(connectionString);
        }
    }
}