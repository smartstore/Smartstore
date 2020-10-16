using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;
using Smartstore.Redis.Caching;
using StackExchange.Redis;

namespace Smartstore.Redis
{
    public class RedisConnectionFactory : Disposable, IRedisConnectionFactory
    {
        private readonly IApplicationContext _appContext;

        private readonly ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>> _multiplexers = new ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>>();
        private readonly ConcurrentDictionary<string, RedisMessageBus> _messageBusses = new ConcurrentDictionary<string, RedisMessageBus>();

        public RedisConnectionFactory(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public ConnectionMultiplexer GetConnection(string connectionString)
        {
            Guard.NotEmpty(connectionString, nameof(connectionString));

            // when using ConcurrentDictionary, multiple threads can create the value
            // at the same time, so we need to pass a Lazy so that it's only 
            // the object which is added that will create a ConnectionMultiplexer,
            // even when a delegate is passed

            return _multiplexers.GetOrAdd(connectionString,
                new Lazy<ConnectionMultiplexer>(() =>
                {
                    Logger.Debug("Connecting Redis. ConnectionString: {0}", connectionString);
                    return ConnectionMultiplexer.Connect(CreateConfigurationOptions(connectionString));
                })).Value;
        }

        public RedisMessageBus GetMessageBus(string connectionString)
        {
            Guard.NotEmpty(connectionString, nameof(connectionString));

            var multiplexer = GetConnection(connectionString);

            return _messageBusses.GetOrAdd(connectionString, key =>
            {
                Logger.Debug("Creating Redis Message Bus. ConnectionString: {0}", connectionString);
                return new RedisMessageBus(multiplexer, _appContext);
            });
        }

        private static ConfigurationOptions CreateConfigurationOptions(string connectionString)
        {
            var options = ConfigurationOptions.Parse(connectionString);

            options.AllowAdmin = true;
            //options.ChannelPrefix = RedisUtility.ScopePrefix + ":";

            return options;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _messageBusses.Clear();

                    foreach (var multiplexer in _multiplexers.Values)
                    {
                        if (multiplexer.IsValueCreated)
                        {
                            var muxer = multiplexer.Value;
                            if (muxer.IsConnected)
                            {
                                muxer.Close();
                                muxer.Dispose();
                            }
                        }
                    }

                    _multiplexers.Clear();
                }
                finally { }
            }
        }
    }
}