using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Engine;
using Smartstore.Threading;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Database cache store.
    /// </summary>
    public class DbCache
    {
        const string KeyPrefix = "efcache:";
        
        private readonly ICacheManager _cache;
        private readonly Work<IRequestCache> _requestCache;
        private readonly AsyncLock _lock = new AsyncLock();

        public DbCache(ICacheManager cache, Work<IRequestCache> requestCache)
        {
            _cache = cache;
            _requestCache = requestCache;
        }

        /// <summary>
        /// Removes all db cache entries.
        /// </summary>
        public void Clear()
        {
            _requestCache.Value.RemoveByPattern(KeyPrefix + '*');
            _cache.RemoveByPattern(KeyPrefix + '*');
        }

        /// <summary>
        /// Removes all db cache entries.
        /// </summary>
        public Task ClearAsync()
        {
            _requestCache.Value.RemoveByPattern(KeyPrefix + '*');
            return _cache.RemoveByPatternAsync(KeyPrefix + '*');
        }

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="key">key to find</param>
        /// <returns>Cached value or <c>null</c></returns>
        public EFCacheData Get(EfCacheKey key, EfCachePolicy policy)
        {
            return _cache.Get<EFCacheData>(BuildKey(key.KeyHash));
        }

        /// <summary>
        /// Puts an item to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        public void Put(EfCacheKey key, EFCacheData value, EfCachePolicy policy)
        {
            value ??= new EFCacheData { IsNull = true };

            var cacheKey = BuildKey(key.KeyHash);

            using (_cache.AcquireKeyLock(cacheKey))
            {
                _cache.Put(cacheKey, value, new CacheEntryOptions().ExpiresIn(policy.ExpirationTimeout));

                foreach (var dependency in key.CacheDependencies)
                {
                    var lookup = GetLookupSet(dependency);
                    lookup.Add(cacheKey);
                }
            }  
        }

        /// <summary>
        /// Puts an item to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        public async Task PutAsync(EfCacheKey key, EFCacheData value, EfCachePolicy policy)
        {
            value ??= new EFCacheData { IsNull = true };

            var cacheKey = BuildKey(key.KeyHash);

            using (await _cache.AcquireAsyncKeyLock(cacheKey))
            {
                await _cache.PutAsync(cacheKey, value, new CacheEntryOptions().ExpiresIn(policy.ExpirationTimeout));

                foreach (var dependency in key.CacheDependencies)
                {
                    var lookup = await GetLookupSetAsync(dependency);
                    lookup.Add(cacheKey);
                }
            }
        }

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="key">Stores information about the computed key of the input LINQ query.</param>
        public void InvalidateCacheDependencies(EfCacheKey key)
        {
            using (_lock.Lock())
            {
                var itemsToInvalidate = new HashSet<string>();

                foreach (var entitySet in key.CacheDependencies)
                {
                    var lookup = GetLookupSet(entitySet, false);
                    if (lookup != null)
                    {
                        itemsToInvalidate.UnionWith(lookup);
                    }
                }

                foreach (var keyToRemove in itemsToInvalidate)
                {
                    _cache.Remove(BuildKey(keyToRemove));
                }
            }
        }

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="key">Stores information about the computed key of the input LINQ query.</param>
        public async Task InvalidateCacheDependenciesAsync(EfCacheKey key)
        {
            using (await _lock.LockAsync())
            {
                var itemsToInvalidate = new HashSet<string>();

                foreach (var entitySet in key.CacheDependencies)
                {
                    var lookup = await GetLookupSetAsync(entitySet, false);
                    if (lookup != null)
                    {
                        itemsToInvalidate.UnionWith(lookup);
                    }
                }

                foreach (var keyToRemove in itemsToInvalidate)
                {
                    await _cache.RemoveAsync(BuildKey(keyToRemove));
                }
            }
        }

        private ISet GetLookupSet(string entitySet, bool create = true)
        {
            var key = GetLookupKeyFor(entitySet);

            if (create)
            {
                return _cache.GetHashSet(key);
            }
            else
            {
                if (_cache.Contains(key))
                {
                    return _cache.GetHashSet(key);
                }
            }

            return null;
        }

        private Task<ISet> GetLookupSetAsync(string entitySet, bool create = true)
        {
            var key = GetLookupKeyFor(entitySet);

            if (create)
            {
                return _cache.GetHashSetAsync(key);
            }
            else
            {
                if (_cache.Contains(key))
                {
                    return _cache.GetHashSetAsync(key);
                }
            }

            return Task.FromResult<ISet>(null);
        }

        private static string GetLookupKeyFor(string entitySet)
        {
            return KeyPrefix + "lookup:" + entitySet;
        }

        private static string BuildKey(string key)
        {
            return KeyPrefix + key;
        }
    }
}