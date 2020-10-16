using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Redis.Caching;
using Smartstore.Threading;

namespace Smartstore.Redis.Threading
{
    public class RedisAsyncState : DefaultAsyncState
    {
        private readonly RedisMessageBus _bus;

        public RedisAsyncState(RedisCacheStore redisCache, RedisMessageBus bus)
            : base(redisCache)
        {
            _bus = bus;

            // Subscribe to async state events (e.g. "Cancel") sent by other nodes in the web farm
            _bus.Subscribe(Channel, OnAsyncStateEvent);
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        private void OnAsyncStateEvent(string channel, string message)
        {
            if (RedisUtility.TryParseEventMessage(message, out var action, out var parameter) && parameter.HasValue())
            {
                // parameter is "key"
                switch (action)
                {
                    case "cancel":
                        if (TryCancel(parameter, true))
                        {
                            Logger.Debug($"AsyncState '{parameter}' canceled by request from another node.");
                        }
                        break;
                    case "removects":
                        if (TryRemoveCancelTokenSource(parameter, true, out _))
                        {
                            Logger.Debug($"AsyncState '{parameter}' removed by request from another node.");
                        }
                        break;
                }
            }
        }

        protected override bool TryRemoveCancelTokenSource(string key, bool successive, out CancellationTokenSource source)
        {
            var removed = base.TryRemoveCancelTokenSource(key, successive, out source);

            // "removed = false" means: the token either did not exist OR was not created by this node.
            // "successive" means: a messagebus subscriber has called this method

            if (!removed && !successive)
            {
                // This server possibly did not create the cancellation token.
                // Call other nodes and let THEM try to remove.

                Logger.Debug($"AsyncState posts '{key}' message to other nodes for state removal.");
                _bus.Publish(Channel, "removects^" + key);
                return true; // TBD: really true?
            }

            return removed;
        }

        protected override bool TryCancel(string key, bool successive = false)
        {
            var canceled = base.TryCancel(key, successive);

            // successive means: a messagebus listener has called this method
            if (!canceled && !successive)
            {
                // This server possibly did not create the cancellation token.
                // Call other nodes and let THEM try to cancel.

                Logger.Debug($"AsyncState posts '{key}' message to other nodes for task cancellation.");
                _bus.Publish(Channel, "cancel^" + key);
                return true; // TBD: really true?
            }

            return canceled;
        }
    }
}