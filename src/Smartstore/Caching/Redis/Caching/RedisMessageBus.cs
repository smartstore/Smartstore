using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;
using Smartstore.Events;
using StackExchange.Redis;

namespace Smartstore.Redis.Caching
{
    public class RedisMessageBus : Disposable, IMessageBus
    {
        private readonly ConnectionMultiplexer _multiplexer;
        private readonly IApplicationContext _appContext;

        private readonly ConcurrentDictionary<string, ConcurrentBag<Action<string, string>>> _handlers;

        public RedisMessageBus(ConnectionMultiplexer multiplexer, IApplicationContext appContext)
        {
            _multiplexer = multiplexer;
            _appContext = appContext;

            // Turn on keyspace events
            // K = Keyspace events, published with __keyspace@<db>__ prefix
            // E = Keyevent events, published with __keyevent@<db>__ prefix.
            // x = Expired events (events generated every time a key expires)
            // e = Evicted events (events generated when a key is evicted for maxmemory)
            // Docs: https://redis.io/topics/notifications
            _multiplexer.GetServer(_multiplexer.GetEndPoints().Single()).ConfigSet("notify-keyspace-events", "KExe");

            _handlers = new ConcurrentDictionary<string, ConcurrentBag<Action<string, string>>>();
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public IDatabase Database 
            => _multiplexer.GetDatabase();

        public long Publish(string channel, string message)
        {
            return Database.Publish(
                channel,
                "_publisher:" + _appContext.EnvironmentIdentifier + "/" + message,
                CommandFlags.FireAndForget);
        }

        public Task<long> PublishAsync(string channel, string message)
        {
            return Database.PublishAsync(
                channel,
                "_publisher:" + _appContext.EnvironmentIdentifier + "/" + message,
                CommandFlags.FireAndForget);
        }

        public void SubscribeToKeyEvent(string keyEvent, Action<string, string> handler, bool ignoreLoopback = true)
        {
            Guard.NotEmpty(keyEvent, nameof(keyEvent));
            Subscribe("__keyevent@" + this.Database.Database + "__:" + keyEvent, handler, ignoreLoopback);
        }

        public void SubscribeToKeySpaceEvent(string keySpace, Action<string, string> handler, bool ignoreLoopback = true)
        {
            Guard.NotEmpty(keySpace, nameof(keySpace));
            Subscribe("__keyspace@" + this.Database.Database + "__:" + keySpace, handler, ignoreLoopback);
        }

        public void Subscribe(string channel, Action<string, string> handler, bool ignoreLoopback = true)
        {
            Guard.NotEmpty(channel, nameof(channel));
            Guard.NotNull(handler, nameof(handler));

            try
            {
                var channelHandlers = _handlers.GetOrAdd(channel, c =>
                {
                    return new ConcurrentBag<Action<string, string>>();
                });

                channelHandlers.Add(handler);

                var subscriber = _multiplexer.GetSubscriber();
                subscriber.Subscribe(channel, (c, m) => RedisSubscriber(c, m, handler, ignoreLoopback));

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured while subscribing to " + channel);
            }
        }

        public async Task SubscribeAsync(string channel, Action<string, string> handler, bool ignoreLoopback = true)
        {
            Guard.NotEmpty(channel, nameof(channel));
            Guard.NotNull(handler, nameof(handler));

            try
            {
                var channelHandlers = _handlers.GetOrAdd(channel, c =>
                {
                    return new ConcurrentBag<Action<string, string>>();
                });

                channelHandlers.Add(handler);

                var subscriber = _multiplexer.GetSubscriber();
                await subscriber.SubscribeAsync(channel, (c, m) => RedisSubscriber(c, m, handler, ignoreLoopback));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured while subscribing to " + channel);
            }
        }

        private void RedisSubscriber(RedisChannel c, RedisValue m, Action<string, string> handler, bool ignoreLoopback = true)
        {
            var msg = m.ToString();

            string publisher = null;
            string message;

            if (msg.StartsWith("_publisher:"))
            {
                // If message starts with 'publisher:', we have to
                // extract the publisher name in order to be able
                // to skip self sent messages
                msg = msg[11..];

                // the message contains the publisher before the first '/'
                var separatorIndex = msg.IndexOf('/');
                publisher = msg.Substring(0, separatorIndex);
                message = msg.Substring(separatorIndex + 1);
            }
            else
            {
                message = msg;
            }

            // ignore self sent messages
            if (ignoreLoopback && publisher.HasValue() && _appContext.EnvironmentIdentifier.Equals(publisher, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Logger.Debug("Channel: {0}, Message: {1}", c.ToString(), message);
            handler(c, message);
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    var subscriber = _multiplexer.GetSubscriber();

                    foreach (var channel in _handlers.Keys)
                    {
                        await subscriber.UnsubscribeAsync(channel, flags: CommandFlags.FireAndForget);
                    }

                    _handlers.Clear();
                }
                catch { }
            }
        }
    }
}
