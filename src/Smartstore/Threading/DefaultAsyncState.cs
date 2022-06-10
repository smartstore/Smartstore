using System.Collections.Concurrent;
using Smartstore.Caching;

namespace Smartstore.Threading
{
    public partial class DefaultAsyncState : IAsyncState
    {
        protected const string Channel = "asyncstate";
        const string KeyPrefix = Channel + ":";

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancelTokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

        public DefaultAsyncState(ICacheStore cache)
        {
            Store = cache;

            if (cache is IMemoryCacheStore memoryStore)
            {
                // Remove the corresponding cancellation token when status info is removed from mem cache.
                memoryStore.Removed += (s, e) =>
                {
                    if (e.Reason != CacheEntryRemovedReason.Replaced && e.Entry.Tag == Channel)
                    {
                        TryRemoveCancelTokenSource(e.Key, false, out _);
                    }
                };
            }

            if (cache is IDistributedCacheStore distributedStore)
            {
                // Remove the corresponding cancellation token when status info gets auto-evicted by distributed cache.
                distributedStore.Expired += (s, e) => TryRemoveCancelTokenSource(e.Key, true, out _);
            }
        }

        public ICacheStore Store { get; }

        public bool Contains<T>(string name = null)
        {
            return Store.Contains(BuildKey<T>(name));
        }

        public T Get<T>(string name = null)
        {
            var entry = GetStateInfo<T>(name);

            if (entry != null)
            {
                return (T)entry.Value;
            }

            return default;
        }

        public async Task<T> GetAsync<T>(string name = null)
        {
            var entry = await GetStateInfoAsync<T>(name);

            if (entry != null)
            {
                return (T)entry.Value;
            }

            return default;
        }

        public virtual IEnumerable<T> GetAll<T>()
        {
            var keyPrefix = BuildKey<T>(null);
            return Store
                .Keys(keyPrefix + "*")
                .Select(key => Store.Get(key))
                .Where(entry => entry?.Value != null)
                .Select(entry => entry.Value)
                .OfType<T>();
        }

        public void Create<T>(T state, string name = null, bool neverExpires = false, CancellationTokenSource cancelTokenSource = default)
        {
            Guard.NotNull(state, nameof(state));

            var key = BuildKey<T>(name);
            var entry = OnCreate(key, state, neverExpires, cancelTokenSource);

            // Add state info to cache store
            Store.Put(key, entry);
        }

        public Task CreateAsync<T>(T state, string name = null, bool neverExpires = false, CancellationTokenSource cancelTokenSource = default)
        {
            Guard.NotNull(state, nameof(state));

            var key = BuildKey<T>(name);
            var entry = OnCreate(key, state, neverExpires, cancelTokenSource);

            // Add state info to cache store
            return Store.PutAsync(key, entry);
        }

        protected virtual CacheEntry OnCreate(string key, object state, bool neverExpires, CancellationTokenSource cancelTokenSource)
        {
            var entry = new CacheEntry
            {
                Key = key,
                Tag = Channel,
                Value = state,
                ValueType = state?.GetType(),
                Priority = CacheEntryPriority.NeverRemove,
                SlidingExpiration = neverExpires ? null : TimeSpan.FromMinutes(15),
                CancelTokenSourceOnRemove = false
            };

            // Register cancel token source
            if (cancelTokenSource != default)
            {
                _cancelTokens.AddOrUpdate(key, cancelTokenSource, (key, oldSource) =>
                {
                    oldSource.Dispose();
                    return cancelTokenSource;
                });
            }

            return entry;
        }

        public bool Update<T>(Action<T> update, string name = null)
        {
            Guard.NotNull(update, nameof(update));

            var entry = GetStateInfo<T>(name);

            if (entry?.Value != null)
            {
                update((T)entry.Value);
                if (Store.IsDistributed)
                {
                    Store.Put(entry.Key, entry);
                }

                return true;
            }

            return false;
        }

        public async Task<bool> UpdateAsync<T>(Action<T> update, string name = null)
        {
            Guard.NotNull(update, nameof(update));

            var entry = await GetStateInfoAsync<T>(name).ConfigureAwait(false);

            if (entry?.Value != null)
            {
                update((T)entry.Value);
                if (Store.IsDistributed)
                {
                    await Store.PutAsync(entry.Key, entry).ConfigureAwait(false);
                }

                return true;
            }

            return false;
        }

        public void Remove<T>(string name = null)
        {
            var key = BuildKey<T>(name);
            TryRemoveCancelTokenSource(key, false, out _);
            Store.Remove(key);
        }

        public Task RemoveAsync<T>(string name = null)
        {
            var key = BuildKey<T>(name);
            TryRemoveCancelTokenSource(key, false, out _);
            return Store.RemoveAsync(key);
        }

        public bool Cancel<T>(string name = null)
        {
            return TryCancel(BuildKey<T>(name));
        }

        /// <summary>
        /// Tries to cancel a token source for a given key.
        /// </summary>
        /// <param name="successive">Pass <c>true</c> if a messagebus subscriber calls this method.</param>
        /// <returns><c>false</c> if the token either did not exist OR was not created by this node.</returns>
        protected virtual bool TryCancel(string key, bool successive = false)
        {
            if (TryGetCancelTokenSource(key, out var source))
            {
                source.Cancel();
                return true;
            }

            return false;
        }

        protected virtual CacheEntry GetStateInfo<T>(string name = null)
        {
            var key = BuildKey<T>(name);

            if (Store.GetTimeToLive(key) != null)
            {
                // Mimic sliding expiration behavior. Extend expiry by 15 min.,
                // but only if an expiration was set before.
                Store.SetTimeToLive(key, TimeSpan.FromMinutes(15));
            }

            return Store.Get(key);
        }

        protected virtual async Task<CacheEntry> GetStateInfoAsync<T>(string name = null)
        {
            var key = BuildKey<T>(name);

            if (await Store.GetTimeToLiveAsync(key).ConfigureAwait(false) != null)
            {
                // Mimic sliding expiration behavior. Extend expiry by 15 min.,
                // but only if an expiration was set before.
                await Store.SetTimeToLiveAsync(key, TimeSpan.FromMinutes(15)).ConfigureAwait(false);
            }

            return await Store.GetAsync(key).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to remove the cancel token source for a given key from the local token store.
        /// </summary>
        /// <param name="successive">Pass <c>true</c> if a messagebus subscriber calls this method.</param>
        /// <returns><c>false</c> if the token either did not exist OR was not created by this node.</returns>
        protected virtual bool TryRemoveCancelTokenSource(string key, bool successive, out CancellationTokenSource source)
        {
            Guard.NotEmpty(key, nameof(key));

            if (_cancelTokens.TryRemove(key, out source))
            {
                source.Dispose();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the cancel token source for a given key from the local token store.
        /// </summary>
        /// <param name="successive">Pass <c>true</c> if a messagebus subscriber has called this method.</param>
        /// <returns><c>false</c> if the token either did not exist OR was not created by this node.</returns>
        protected virtual bool TryGetCancelTokenSource(string key, out CancellationTokenSource source)
        {
            Guard.NotEmpty(key, nameof(key));

            return _cancelTokens.TryGetValue(key, out source);
        }

        protected string BuildKey<T>(string name)
        {
            return BuildKey(typeof(T), name);
        }

        protected virtual string BuildKey(Type type, string name)
        {
            return KeyPrefix + type.FullName + name.LeftPad(pad: ':');
        }
    }
}