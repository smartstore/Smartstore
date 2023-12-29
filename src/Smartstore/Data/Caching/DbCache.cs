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
        private readonly AsyncLock _lock = new();

        public DbCache()
        {
            _cache = EngineContext.Current.Application.Services.Resolve<ICacheFactory>().GetMemoryCache();
            _cache.Expired += OnEntryExpired;
        }

        private void OnEntryExpired(object sender, CacheEntryExpiredEventArgs e)
        {
            // Remove the expired key from all lookup sets
            if (e.Key.StartsWith(KeyPrefix))
            {
                // Get all existing lookup sets
                var keys = _cache.Keys(BuildLookupKeyFor("*"));
                foreach (var key in keys)
                {
                    // Remove entry key from lookup
                    _cache.GetHashSet(key).Remove(e.Key);
                }
            }
        }

        public void Clear()
        {
            _cache.RemoveByPattern(BuildKey("*"));
            _cache.RemoveByPattern(BuildLookupKeyFor("*"));
        }

        public async Task ClearAsync()
        {
            await _cache.RemoveByPatternAsync(BuildKey("*"));
            await _cache.RemoveByPatternAsync(BuildLookupKeyFor("*"));
        }

        public DbCacheEntry Get(DbCacheKey key, DbCachingPolicy policy)
        {
            return _cache.Get<DbCacheEntry>(BuildKey(key.Key));
        }

        public void Put(DbCacheKey key, DbCacheEntry value, DbCachingPolicy policy)
        {
            var cacheKey = BuildKey(key.Key);

            using (_cache.GetLock(cacheKey).Acquire(TimeSpan.FromSeconds(5)))
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
                .Select(QueryKeyGenerator.GenerateDependencyKey)
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
            var key = BuildLookupKeyFor(entitySet);

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

        private static string BuildLookupKeyFor(string entitySet)
        {
            return KeyPrefix + "lookup:" + entitySet;
        }

        private static string BuildKey(string key)
        {
            return KeyPrefix + key;
        }
    }
}