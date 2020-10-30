using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Caching;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Database cache store.
    /// </summary>
    public class DbCache
    {
        const string KeyPrefix = "efcache:";
        
        private readonly ICacheManager _cache;

        public DbCache(ICacheManager cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Removes all db cache entries.
        /// </summary>
        public void Clear()
        {
            _cache.RemoveByPattern(KeyPrefix + '*');
        }

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="key">key to find</param>
        /// <returns>Cached value or <c>null</c></returns>
        public EfCachedData Get(EfCacheKey key, EfCachePolicy policy)
        {
            return null;
        }

        /// <summary>
        /// Puts an item to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        public void Put(EfCacheKey key, EfCachedData value, EfCachePolicy policy)
        {
        }

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="key">Stores information about the computed key of the input LINQ query.</param>
        public void InvalidateCacheDependencies(EfCacheKey key)
        {
        }

        private string BuildKey(string key)
        {
            return KeyPrefix + key;
        }
    }
}