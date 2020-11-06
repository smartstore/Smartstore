using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Caching;
using Smartstore.Engine;
using Smartstore.Threading;

namespace Smartstore.Data.Caching
{
    public class DbCache : IDbCache
    {
        const string KeyPrefix = "efcache:";

        private readonly ICacheManager _cache;
        private readonly AsyncLock _lock = new AsyncLock();

        public DbCache()
        {
            _cache = EngineContext.Current.Application.Services.Resolve<ICacheManager>();
        }

        public void Clear()
        {
            _cache.RemoveByPattern(KeyPrefix + '*');
        }

        public Task ClearAsync()
        {
            return _cache.RemoveByPatternAsync(KeyPrefix + '*');
        }

        public DbCacheEntry Get(DbCacheKey key, DbCachingPolicy policy)
        {
            return _cache.Get<DbCacheEntry>(BuildKey(key.KeyHash));
        }

        public void Put(DbCacheKey key, DbCacheEntry value, DbCachingPolicy policy)
        {
            var cacheKey = BuildKey(key.KeyHash);

            using (_cache.AcquireKeyLock(cacheKey))
            {
                _cache.Put(cacheKey, value, new CacheEntryOptions().ExpiresIn(policy.ExpirationTimeout.Value));

                foreach (var set in key.EntitySets)
                {
                    var lookup = GetLookupSet(set);
                    lookup.Add(cacheKey);
                }
            }
        }

        public void Invalidate(params Type[] entityTypes)
        {
            if (entityTypes.Length == 0)
                return;
            
            var sets = entityTypes
                .Distinct()
                .Select(x => QueryKeyGenerator.GenerateDependencyKey(x))
                .ToArray();
            
            using (_lock.Lock())
            {
                var itemsToInvalidate = new HashSet<string>();

                foreach (var entitySet in sets)
                {
                    var lookup = GetLookupSet(entitySet, false);
                    if (lookup != null)
                    {
                        itemsToInvalidate.UnionWith(lookup);
                    }
                }

                foreach (var key in itemsToInvalidate)
                {
                    InvalidateItem(key);
                }
            }
        }

        private void InvalidateItem(string key)
        {
            if (_cache.Contains(key))
            {
                var entry = _cache.Get<DbCacheEntry>(key, true);
                if (entry != null)
                {
                    InvalidateItem(key, entry);
                }
            }
        }

        protected void InvalidateItem(string key, DbCacheEntry entry)
        {
            // Remove item itself from cache
            _cache.Remove(key);

            // Remove this key in all lookups
            foreach (var set in entry.Key.EntitySets)
            {
                var lookup = GetLookupSet(set, false);
                if (lookup != null)
                {
                    lookup.Remove(key);
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