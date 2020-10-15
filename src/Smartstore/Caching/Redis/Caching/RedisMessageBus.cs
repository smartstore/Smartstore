using System;
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

        public void SubscribeToKeyEvent(string keyEvent, Action<string, string> handler)
        {
            Guard.NotEmpty(keyEvent, nameof(keyEvent));
            Subscribe("__keyevent@" + this.Database.Database + "__:" + keyEvent, handler);
        }

        public void SubscribeToKeySpaceEvent(string keySpace, Action<string, string> handler)
        {
            Guard.NotEmpty(keySpace, nameof(keySpace));
            Subscribe("__keyspace@" + this.Database.Database + "__:" + keySpace, handler);
        }

        public void Subscribe(string channel, Action<string, string> handler)
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
                subscriber.Subscribe(channel, (c, m) => RedisSubscriber(c, m, handler));

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured while subscribing to " + channel);
            }
        }

        public async Task SubscribeAsync(string channel, Action<string, string> handler)
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
                await subscriber.SubscribeAsync(channel, (c, m) => RedisSubscriber(c, m, handler)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured while subscribing to " + channel);
            }
        }

        private void RedisSubscriber(RedisChannel c, RedisValue m, Action<string, string> handler)
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
            if (publisher.HasValue() && _appContext.EnvironmentIdentifier.Equals(publisher, StringComparison.OrdinalIgnoreCase))
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
