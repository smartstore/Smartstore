namespace Smartstore.Caching
{
    /// <summary>
    /// Responsible for resolving cache manager instances.
    /// </summary>
    public interface ICacheFactory
    {
        /// <summary>
        /// Resolves a singleton cache manager instance that only interacts with the current <see cref="IMemoryCacheStore"/> implementation. 
        /// </summary>
        ICacheManager GetMemoryCache();

        /// <summary>
        /// Resolves a singleton cache manager instance that only interacts with the current <see cref="IDistributedCacheStore"/> implementation.
        /// </summary>
        /// <returns>
        /// A cache manager instance or <c>null</c> if no <see cref="IDistributedCacheStore"/> implementation is registered.
        /// </returns>
        ICacheManager GetDistributedCache();

        /// <summary>
        /// Resolves a composite multi-level cache manager instance that interacts with both
        /// <see cref="IMemoryCacheStore"/> and <see cref="IDistributedCacheStore"/> implementations.
        /// If no <see cref="IDistributedCacheStore"/> implementation is registered, the inner cache stores
        /// will only contain <see cref="IMemoryCacheStore"/>.
        /// </summary>
        ICacheManager GetHybridCache();
    }
}
