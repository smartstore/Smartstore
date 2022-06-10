namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Database cache store.
    /// </summary>
    public interface IDbCache
    {
        /// <summary>
        /// Removes all db cache entries.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes all db cache entries.
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="key">key to find</param>
        /// <returns>Cached value or <c>null</c></returns>
        DbCacheEntry Get(DbCacheKey key, DbCachingPolicy policy);

        /// <summary>
        /// Puts an item to the cache.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        void Put(DbCacheKey key, DbCacheEntry value, DbCachingPolicy policy);

        /// <summary>
        /// Invalidates all cache entries which are dependent on any of the specified entity types.
        /// </summary>
        void Invalidate(params Type[] entityTypes);
    }
}
