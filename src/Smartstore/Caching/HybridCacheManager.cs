
using Smartstore.Engine;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Caching
{
    public class HybridCacheManager : ICacheManager
    {
        const string LockRecursionExceptionMessage = "Acquiring identical cache items recursively is not supported. Key: {0}";

        private readonly ICacheStore[] _stores;
        private readonly Work<ICacheScopeAccessor> _scopeAccessor;
        private readonly bool _isMultiLevel;
        private readonly bool _isDistributed;
        private readonly int _lastIndex;

        public HybridCacheManager(IEnumerable<ICacheStore> stores, Work<ICacheScopeAccessor> scopeAccessor)
        {
            // Distributed cache store must come last
            // Performance Info: Iterating over an array with "for" instead of "foreach" has provenly no perf benefit (in contrary to list iteration).
            _stores = stores.Distinct().OrderBy(x => x.IsDistributed).ToArray();

            if (_stores.LastOrDefault() is IDistributedCacheStore distributedStore)
            {
                // Listen to auto expirations/evictions in distributed store
                // so that we can remove the key from the local memory caches after expiration.
                distributedStore.Expired += OnDistributedCacheEntryExpired;
                _isDistributed = true;
            }
            else if (_stores.FirstOrDefault() is IMemoryCacheStore memoryStore)
            {
                // Invoke Expired event when eviction reason is expiration.
                memoryStore.Removed += OnMemoryCacheEntryRemoved;
            }

            _scopeAccessor = scopeAccessor;
            _isMultiLevel = _stores.Length > 1;
            _lastIndex = _stores.Length - 1;
        }

        #region Events

        public event EventHandler<CacheEntryExpiredEventArgs> Expired;

        private void OnDistributedCacheEntryExpired(object sender, CacheEntryExpiredEventArgs e)
        {
            if (e.Key.HasValue())
            {
                // When a cache entry expires in a distributed store,
                // remove the key from all memory stores.
                foreach (var store in _stores)
                {
                    if (store is IMemoryCacheStore)
                    {
                        store.Remove(e.Key);
                    }
                }

                // Raise expired event
                Expired?.Invoke(sender, e);
            }
        }

        private void OnMemoryCacheEntryRemoved(object sender, CacheEntryRemovedEventArgs e)
        {
            if (e.Reason >= CacheEntryRemovedReason.Expired)
            {
                Expired?.Invoke(sender, e);
            }
        }

        #endregion

        #region Read

        public bool Contains(string key)
        {
            foreach (var store in _stores)
            {
                if (store.Contains(key))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> ContainsAsync(string key)
        {
            foreach (var store in _stores)
            {
                if (await store.ContainsAsync(key))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<string> Keys(string pattern = "*")
        {
            // Get only from LAST store
            return _stores[_lastIndex].Keys(pattern);
        }

        public IAsyncEnumerable<string> KeysAsync(string pattern = "*")
        {
            // Get only from LAST store
            return _stores[_lastIndex].KeysAsync(pattern);
        }

        public ISet GetHashSet(string key, Func<IEnumerable<string>> acquirer = null)
        {
            Guard.NotEmpty(key, nameof(key));

            // Get only from LAST store
            return _stores[_lastIndex].GetHashSet(key, acquirer);
        }

        public Task<ISet> GetHashSetAsync(string key, Func<Task<IEnumerable<string>>> acquirer = null)
        {
            Guard.NotEmpty(key, nameof(key));

            // INFO: Get only from LAST store
            return _stores[_lastIndex].GetHashSetAsync(key, acquirer);
        }

        public T Get<T>(string key, bool independent = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var entry = GetInternal(key, independent, false).Await().Entry;
            if (entry?.Value != null)
            {
                return (T)entry.Value;
            }

            return default;
        }

        public async Task<T> GetAsync<T>(string key, bool independent = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var entry = (await GetInternal(key, independent, true)).Entry;
            if (entry?.Value != null)
            {
                return (T)entry.Value;
            }

            return default;
        }

        public bool TryGet<T>(string key, out T value)
        {
            Guard.NotEmpty(key, nameof(key));

            value = default;
            var entry = GetInternal(key, false, false).Await().Entry;
            if (entry != null)
            {
                value = (T)entry.Value;
            }

            return entry != null;
        }

        public async Task<AsyncOut<T>> TryGetAsync<T>(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            var entry = (await GetInternal(key, false, true)).Entry;
            if (entry != null)
            {
                return new AsyncOut<T>(true, (T)entry.Value);
            }

            return AsyncOut<T>.Empty;
        }

        public T Get<T>(string key, Func<CacheEntryOptions, T> acquirer, bool independent = false, bool allowRecursion = false)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(acquirer, nameof(acquirer));

            var entry = GetInternal(key, independent, false).Await().Entry;
            if (entry != null)
            {
                return entry.Value == null ? default : (T)entry.Value;
            }

            if (!allowRecursion && _scopeAccessor.Value.HasScope(key))
            {
                throw new LockRecursionException(LockRecursionExceptionMessage.FormatInvariant(key));
            }

            T value = default;

            // Get the (semaphore) locker specific to this key from LAST store.
            // Atomic operation must be outer locked
            using (GetLock(key).Acquire(TimeSpan.FromSeconds(5)))
            {
                // Check again
                entry = GetInternal(key, independent, false).Await().Entry;

                if (entry != null)
                {
                    value = entry.Value == null ? default : (T)entry.Value;
                }
                else
                {
                    // Create value by invoking acquirer
                    var scope = !allowRecursion ? _scopeAccessor.Value.BeginScope(key) : ActionDisposable.Empty;
                    using (scope)
                    {
                        // Invoke acquirer
                        var options = new CacheEntryOptions();
                        value = acquirer(options);

                        // Determine dependency entries and enlist them
                        var dependencies = !allowRecursion ? _scopeAccessor.Value.Current?.Dependencies : null;
                        if (dependencies != null)
                        {
                            options.DependsOn(dependencies.ToArray());
                        }

                        // Put to cache stores
                        Put(key, value, options);
                        return value;
                    }
                }
            }

            return value;
        }

        public async Task<T> GetAsync<T>(string key, Func<CacheEntryOptions, Task<T>> acquirer, bool independent = false, bool allowRecursion = false)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(acquirer, nameof(acquirer));

            var entry = (await GetInternal(key, independent, true)).Entry;
            if (entry != null)
            {
                return entry.Value == null ? default : (T)entry.Value;
            }

            if (!allowRecursion && _scopeAccessor.Value.HasScope(key))
            {
                throw new LockRecursionException(LockRecursionExceptionMessage.FormatInvariant(key));
            }

            T value = default;

            // Get the (semaphore) locker specific to this key from LAST store.
            // Atomic operation must be outer locked
            await using (await GetLock(key).AcquireAsync(TimeSpan.FromSeconds(5)))
            {
                // Check again
                entry = (await GetInternal(key, independent, true)).Entry;

                if (entry != null)
                {
                    value = entry.Value == null ? default : (T)entry.Value;
                }
                else
                {
                    // Create value by invokin acquirer
                    var scope = !allowRecursion ? _scopeAccessor.Value.BeginScope(key) : ActionDisposable.Empty;
                    using (scope)
                    {
                        // Invoke acquirer
                        var options = new CacheEntryOptions();
                        value = await acquirer(options);

                        // Determine dependency entries and enlist them
                        var dependencies = !allowRecursion ? _scopeAccessor.Value.Current?.Dependencies : null;
                        if (dependencies != null)
                        {
                            options.DependsOn(dependencies.ToArray());
                        }

                        // Put to cache stores
                        await PutAsync(key, value, options);
                        return value;
                    }
                }
            }

            return value;
        }

        public virtual TimeSpan? GetTimeToLive(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            // Get from last store.
            return _stores[_lastIndex].GetTimeToLive(key);
        }

        public virtual Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            // Get from last store.
            return _stores[_lastIndex].GetTimeToLiveAsync(key);
        }

        public virtual void SetTimeToLive(string key, TimeSpan? duration)
        {
            Guard.NotEmpty(key, nameof(key));

            // Update in last store only and rely on message bus to propagate expiration.
            _stores[_lastIndex].SetTimeToLive(key, duration);
        }

        public virtual Task SetTimeToLiveAsync(string key, TimeSpan? duration)
        {
            Guard.NotEmpty(key, nameof(key));

            // Update in last store only and rely on message bus to propagate expiration.
            return _stores[_lastIndex].SetTimeToLiveAsync(key, duration);
        }

        #endregion

        #region Write

        public void Put(string key, object value, CacheEntryOptions options = null)
        {
            Guard.NotEmpty(key, nameof(key));

            // Reverse order
            for (int i = _lastIndex; i >= 0; i--)
            {
                var entry = CreateCacheEntry(key, value, options);

                if (_isDistributed && i < _lastIndex)
                {
                    // Rely on message bus from distributed store to propagate expiration event to this downstream store.
                    entry.ApplyTimeExpirationPolicy = false;
                }

                _stores[i].Put(key, entry);
            }
        }

        public async Task PutAsync(string key, object value, CacheEntryOptions options = null)
        {
            Guard.NotEmpty(key, nameof(key));

            // Reverse order
            for (int i = _lastIndex; i >= 0; i--)
            {
                var entry = CreateCacheEntry(key, value, options);

                if (_isDistributed && i < _lastIndex)
                {
                    // Rely on message bus from distributed store to propagate expiration event to this downstream store.
                    entry.ApplyTimeExpirationPolicy = false;
                }

                await _stores[i].PutAsync(key, entry);
            }
        }

        public void Remove(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            // Reverse order
            for (int i = _lastIndex; i >= 0; i--)
            {
                _stores[i].Remove(key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            // Reverse order
            for (int i = _lastIndex; i >= 0; i--)
            {
                await _stores[i].RemoveAsync(key);
            }
        }

        public long RemoveByPattern(string pattern)
        {
            Guard.NotEmpty(pattern, nameof(pattern));

            var counts = new long[_stores.Length];

            // Reverse order
            for (int i = _lastIndex; i >= 0; i--)
            {
                counts[i] = _stores[i].RemoveByPattern(pattern);
            }

            return counts.Max();
        }

        public async Task<long> RemoveByPatternAsync(string pattern)
        {
            Guard.NotEmpty(pattern, nameof(pattern));

            var counts = new long[_stores.Length];

            // Reverse order
            for (int i = _lastIndex; i >= 0; i--)
            {
                counts[i] = await _stores[i].RemoveByPatternAsync(pattern);
            }

            return counts.Max();
        }

        public IDistributedLock GetLock(string key)
        {
            return _stores[_lastIndex].GetLock(key);
        }

        public void Clear()
        {
            foreach (var store in _stores)
            {
                store.Clear();
            }
        }

        public async Task ClearAsync()
        {
            foreach (var store in _stores)
            {
                await store.ClearAsync();
            }
        }

        #endregion

        #region Internal Utils

        public static CacheEntry CreateCacheEntry(string key, object value, CacheEntryOptions options)
        {
            var entry = options?.AsEntry(key, value) ?? new CacheEntry
            {
                Key = key,
                Value = value,
                ValueType = value?.GetType()
            };

            return entry;
        }

        private async ValueTask<(CacheEntry Entry, ICacheStore Store, int Index)> GetInternal(string key, bool independent, bool async)
        {
            int index = 0;

            foreach (var store in _stores)
            {
                var entry = async ? await store.GetAsync(key) : store.Get(key);
                if (entry != null)
                {
                    // Make the parent scope's entry depend on this
                    if (!independent)
                    {
                        _scopeAccessor.Value.PropagateKey(key);
                    }

                    // Put found entry to PREVIOUS cache stores.
                    int i = index - 1;
                    while (i >= 0)
                    {
                        var entryClone = entry.Clone();
                        // Rely on message bus from distributed store to propagate expiration event to this downstream store.
                        entryClone.ApplyTimeExpirationPolicy = false;

                        if (async)
                        {
                            await _stores[i].PutAsync(key, entryClone);
                        }
                        else
                        {
                            _stores[i].Put(key, entryClone);
                        }

                        i--;
                    }

                    if (index < _lastIndex
                        && entry.SlidingExpiration.HasValue
                        && _stores[_lastIndex] is IDistributedCacheStore distributedStore)
                    {
                        // Refresh last access time and TTL in upstream distributed store (but only when entry has sliding expiration)
                        // Fire & forget
                        if (async)
                        {
                            _ = distributedStore.RefreshAsync(entry);
                        }
                        else
                        {
                            distributedStore.Refresh(entry);
                        }

                    }

                    return (entry, store, index);
                }

                index++;
            }

            return (null, null, -1);
        }

        #endregion
    }
}
