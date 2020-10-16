using System;
using System.Data.Common;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.Redis.Caching;
using Smartstore.Threading;

namespace Smartstore.Redis.Threading
{
    public class RedisAsyncState : DefaultAsyncState
    {
        private readonly IAsyncState _localState;
        private readonly RedisMessageBus _bus;

        public RedisAsyncState(RedisCacheStore redisCache, IAsyncState localState, RedisMessageBus bus)
            : base(redisCache)
        {
            _localState = localState;
            _bus = bus;

            // Subscribe to key events triggered by Redis on item expiration.
            // (The "Expired" event is only triggered by distributed caches like Redis).
            // Try to remove the entry from local state store.
            redisCache.Expired += (o, e) => _localState.Store.Remove(e.Key);

            // Subscribe to async state events (e.g. "Cancel") sent by other nodes in the web farm
            _bus.Subscribe("asyncstate", OnAsyncStateEvent);
        }

        private void OnAsyncStateEvent(string channel, string message)
        {
            if (RedisUtility.TryParseEventMessage(message, out var action, out var parameter) && parameter.HasValue())
            {
                // parameter is "key"
                switch (action)
                {
                    case "cancel":
                        // TODO: (core) I don't like this code. It's redundant.
                        var entry = _localState.Store.Get(parameter);
                        if (entry != null && !entry.CancellationTokenSource.IsCancellationRequested)
                        {
                            entry.CancellationTokenSource.Cancel();
                            Logger.Debug($"AsyncState '{parameter}' canceled by request from another node.");
                        }
                        break;
                    case "remove":
                        _localState.Store.Remove(parameter);
                        Logger.Debug($"AsyncState '{parameter}' removed by request from another node.");
                        break;
                }
            }
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override void Create<T>(T state, string name = null, bool neverExpires = false, CancellationTokenSource cancelTokenSource = null)
        {
            base.Create(state, name, neverExpires, cancelTokenSource);

            // Put also to local store
            _localState.Create<T>(state, name, neverExpires, cancelTokenSource);
        }

        public override bool Update<T>(Action<T> update, string name = null)
        {
            var updated = base.Update(update, name);

            // Update also in local store (if created on this node)
            _localState.Update<T>(update, name);

            return updated;
        }

        public override void Remove<T>(string name = null)
        {
            base.Remove<T>(name);

            // Remove also from local store (if created on this node)
            var removed = _localState.Contains<T>(name);
            _localState.Remove<T>(name);

            if (!removed)
            {
                // This node possibly did not create the entry.
                // Call other nodes and let THEM try to remove.
                var key = BuildKey<T>(name);
                Logger.Debug($"AsyncState posts '{key}' message to other nodes for state removal.");
                _bus.Publish("asyncstate", "remove^" + key);
            }
        }

        public override bool Cancel<T>(string name = null)
        {
            var canceled = base.Cancel<T>(name);

            if (canceled)
            {
                // CancellationTokenSources in Redis are volatile. Here "canceled = true" only
                // indicates that the state entry existed in Redis.
                // We have to (try to) cancel the source on current node.
                canceled = _localState.Cancel<T>(name);
                if (!canceled)
                {
                    // But this node possibly did not create the entry.
                    // Call other nodes and let THEM try to cancel.
                    var key = BuildKey<T>(name);
                    Logger.Debug($"AsyncState posts '{key}' message to other nodes for task cancellation.");
                    _bus.Publish("asyncstate", "cancel^" + key);
                    return true; // TBD: really true?
                }
            }

            return canceled;
        }

        //private static string NormalizeKey(string redisKey)
        //{
        //    return redisKey[KeyPrefix.Length..];
        //}
    }
}