using Smartstore.Engine;

namespace Smartstore.Caching
{
    public class DefaultCacheFactory : ICacheFactory
    {
        private readonly ICacheManager _memoryCache;
        private readonly ICacheManager _distributedCache;
        private readonly ICacheManager _hybridCache;

        public DefaultCacheFactory(IEnumerable<ICacheStore> stores, ICacheManager cacheManager, Work<ICacheScopeAccessor> scopeAccessor)
        {
            // Always the default registration
            _hybridCache = cacheManager;

            var memStores = stores.OfType<IMemoryCacheStore>();
            _memoryCache = memStores.Any()
                ? new HybridCacheManager(memStores, scopeAccessor)
                : NullCache.Instance;

            var distributedStores = stores.OfType<IDistributedCacheStore>();
            if (distributedStores.Any())
            {
                _distributedCache = new HybridCacheManager(distributedStores, scopeAccessor);
            }
        }

        public ICacheManager GetMemoryCache()
            => _memoryCache;

        public ICacheManager GetDistributedCache()
            => _distributedCache;

        public ICacheManager GetHybridCache()
            => _hybridCache;
    }
}