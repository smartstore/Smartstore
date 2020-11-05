using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Caching;
using Smartstore.Engine;
using Smartstore.Threading;

namespace Smartstore.Data.Caching2
{
    /// <summary>
    /// Database cache store.
    /// </summary>
    public class DbCache
    {
        const string KeyPrefix = "efcache:";

        private readonly ICacheManager _cache;
        private readonly AsyncLock _lock = new AsyncLock();

        public DbCache()
        {
            _cache = EngineContext.Current.Application.Services.Resolve<ICacheManager>();
        }

        /// <summary>
        /// Removes all db cache entries.
        /// </summary>
        public void Clear()
        {
            _cache.RemoveByPattern(KeyPrefix + '*');
        }

        /// <summary>
        /// Removes all db cache entries.
        /// </summary>
        public Task ClearAsync()
        {
            return _cache.RemoveByPatternAsync(KeyPrefix + '*');
        }

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="key">key to find</param>
        /// <returns>Cached value or <c>null</c></returns>
        public DbCacheEntry Get(DbCacheKey key, DbCachingPolicy policy)
        {
            return _cache.Get<DbCacheEntry>(BuildKey(key.KeyHash));
        }

        /// <summary>
        /// Puts an item to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        public void Put(DbCacheKey key, DbCacheEntry value, DbCachingPolicy policy)
        {
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
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="key">Stores information about the computed key of the input LINQ query.</param>
        public void InvalidateCacheDependencies(DbCacheKey key)
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
        public async Task InvalidateCacheDependenciesAsync(DbCacheKey key)
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