using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Engine;
using Smartstore.Utilities;

namespace Smartstore.Caching
{
    public class HybridCacheManager : ICacheManager
    {
        const string LockRecursionExceptionMessage = "Acquiring identical cache items recursively is not supported. Key: {0}";

        private readonly ICacheStore[] _stores;
        private readonly Work<ICacheScopeAccessor> _scopeAccessor;

        public HybridCacheManager(IEnumerable<ICacheStore> stores, Work<ICacheScopeAccessor> scopeAccessor)
        {
            // Distributed cache store must come last
            _stores = stores.Distinct().OrderBy(x => x.IsDistributed).ToArray();

            if (_stores.LastOrDefault() is IDistributedCacheStore distributedStore)
            {
                // Listen to auto expirations/evictions in distributed store,
                // so that we can remove the key from the local memory caches after expiration.
                distributedStore.Expired += OnDistributedCacheEntryExpired;
            }

            _scopeAccessor = scopeAccessor;
        }

        #region Events

        private void OnDistributedCacheEntryExpired(object sender, CacheEntryExpiredEventArgs e)
        {
            if (e.Key.HasValue())
            {
                // When a cache entry expires in a distributed store,
                // remove the key from all memory stores.
                _stores.OfType<IMemoryCacheStore>().Each(x => x.Remove(e.Key));
            }
        }

        #endregion

        #region Read

        public bool Contains(string key)
        {
            return _stores.Any(x => x.Contains(key));
        }

        public Task<bool> ContainsAsync(string key)
        {
            return _stores.AnyAsync(x => x.ContainsAsync(key));
        }

        public IEnumerable<string> Keys(string pattern = "*")
        {
            // INFO: Reverse order
            return _stores.Reverse().Select(x => x.Keys(pattern)).FirstOrDefault();
        }

        public IAsyncEnumerable<string> KeysAsync(string pattern = "*")
        {
            // INFO: Reverse order
            return _stores.Reverse().Select(x => x.KeysAsync(pattern)).FirstOrDefault();
        }

        public ISet GetHashSet(string key, Func<IEnumerable<string>> acquirer = null)
        {
            Guard.NotEmpty(key, nameof(key));

            // INFO: Get only from LAST store
            return _stores.Last().GetHashSet(key, acquirer);
        }

        public Task<ISet> GetHashSetAsync(string key, Func<Task<IEnumerable<string>>> acquirer = null)
        {
            Guard.NotEmpty(key, nameof(key));

            // INFO: Get only from LAST store
            return _stores.Last().GetHashSetAsync(key, acquirer);
        }

        public T Get<T>(string key, bool independent = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var entry = GetInternal(key, independent).Entry;
            if (entry?.Value != null)
            {
                return (T)entry?.Value;
            }
            
            return default;
        }

        public async Task<T> GetAsync<T>(string key, bool independent = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var entry = (await GetInternalAsync(key, independent)).Entry;
            if (entry?.Value != null)
            {
                return (T)entry?.Value;
            }

            return default;
        }

        public T Get<T>(string key, Func<CacheEntryOptions, T> acquirer, bool independent = false, bool allowRecursion = false)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(acquirer, nameof(acquirer));

            var entry = GetInternal(key, independent).Entry;
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
            using (_stores.Last().AcquireKeyLock(key))
            {
                // Check again
                entry = GetInternal(key, independent).Entry;

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

            var entry = (await GetInternalAsync(key, independent)).Entry;
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
            using (await _stores.Last().AcquireAsyncKeyLock(key))
            {
                // Check again
                entry = (await GetInternalAsync(key, independent)).Entry;

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

        #endregion


        #region Write

        public void Put(string key, object value, CacheEntryOptions options = null)
        {
            Guard.NotEmpty(key, nameof(key));

            var entry = CreateCacheEntry(key, value, options);

            _stores.Each(x => x.Put(key, entry));
        }

        public Task PutAsync(string key, object value, CacheEntryOptions options = null)
        {
            Guard.NotEmpty(key, nameof(key));

            var entry = CreateCacheEntry(key, value, options);

            return _stores.EachAsync(x => x.PutAsync(key, entry));
        }

        public void Remove(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            // INFO: Reverse order
            _stores.Reverse().Each(x => x.Remove(key));
        }

        public Task RemoveAsync(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            // INFO: Reverse order
            return _stores.Reverse().EachAsync(x => x.RemoveAsync(key));
        }

        public long RemoveByPattern(string pattern)
        {
            Guard.NotEmpty(pattern, nameof(pattern));

            // INFO: Reverse order
            return _stores.Reverse().Max(x => x.RemoveByPattern(pattern));
        }

        public async Task<long> RemoveByPatternAsync(string pattern)
        {
            Guard.NotEmpty(pattern, nameof(pattern));

            // INFO: Reverse order
            return await _stores.Reverse().Max(async (x) => await x.RemoveByPatternAsync(pattern)).ConfigureAwait(false);
        }

        public void Clear()
        {
            _stores.Each(x => x.Clear());
        }

        public Task ClearAsync()
        {
            return _stores.EachAsync(x => x.ClearAsync());
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

        private static CacheEntry CloneEntry(CacheEntry source)
        {
            // INFO: never copy CancelTokenSource
            return new CacheEntry
            {
                Key = source.Key,
                Value = source.Value,
                ValueType = source.ValueType,
                Dependencies = source.Dependencies,
                LastAccessedOn = source.LastAccessedOn,
                CachedOn = DateTime.UtcNow,
                Duration = source.TimeToLive
            };
        }

        private (CacheEntry Entry, ICacheStore Store, int Index) GetInternal(string key, bool independent)
        {
            int index = 0;

            foreach (var store in _stores)
            {
                var entry = store.Get(key);
                if (entry != null)
                {
                    // Make the parent scope's entry depend on this
                    if (!independent)
                    {
                        _scopeAccessor.Value.PropagateKey(key);
                    }

                    // Entry found. Put found entry to PREVIOUS cache stores.
                    int i = index - 1;
                    while (i >= 0)
                    {
                        _stores[i].Put(key, CloneEntry(entry));
                        i--;
                    }

                    // INFO: has no effect for distributed caches.
                    entry.LastAccessedOn = DateTimeOffset.UtcNow;

                    return (entry, store, index);
                }

                index++;
            }
            
            return (null, null, -1);
        }

        private async ValueTask<(CacheEntry Entry, ICacheStore Store, int Index)> GetInternalAsync(string key, bool independent)
        {
            int index = 0;

            foreach (var store in _stores)
            {
                var entry = await store.GetAsync(key);
                if (entry != null)
                {
                    // Make the parent scope's entry depend on this
                    if (!independent)
                    {
                        _scopeAccessor.Value.PropagateKey(key);
                    }

                    // Entry found. Put found entry to PREVIOUS cache stores.
                    int i = index - 1;
                    while (i >= 0)
                    {
                        await _stores[i].PutAsync(key, CloneEntry(entry));
                        i--;
                    }

                    // INFO: has no effect for distributed caches.
                    entry.LastAccessedOn = DateTimeOffset.UtcNow;

                    return (entry, store, index);
                }

                index++;
            }

            return (null, null, -1);
        }

        #endregion
    }
}
