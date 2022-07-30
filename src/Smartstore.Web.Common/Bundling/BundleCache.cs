using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Smartstore.Web.Bundling
{
    public struct BundleCacheKey
    {
        public string Key { get; init; }
        public IDictionary<string, string> Fragments { get; init; }
        public static implicit operator string(BundleCacheKey key) => key.Key;
        public static implicit operator BundleCacheKey(string key) => new() { Key = key, Fragments = new Dictionary<string, string>() };
    }

    public interface IBundleCache
    {
        Task<BundleResponse> GetResponseAsync(BundleCacheKey cacheKey, Bundle bundle);

        Task PutResponseAsync(BundleCacheKey cacheKey, Bundle bundle, BundleResponse response);

        Task RemoveResponseAsync(BundleCacheKey cacheKey);

        Task ClearAsync();
    }

    public class BundleCache : IBundleCache
    {
        private readonly IMemoryCache _memCache;
        private readonly IBundleDiskCache _diskCache;
        private readonly IOptionsMonitor<BundlingOptions> _options;

        public BundleCache(IMemoryCache memCache, IBundleDiskCache diskCache, IOptionsMonitor<BundlingOptions> options)
        {
            _memCache = memCache;
            _diskCache = diskCache;
            _options = options;
        }

        public async Task<BundleResponse> GetResponseAsync(BundleCacheKey cacheKey, Bundle bundle)
        {
            Guard.NotNull(cacheKey.Key, nameof(cacheKey.Key));
            Guard.NotNull(bundle, nameof(bundle));

            // Memory cache
            var memCacheKey = BuildScopedCacheKey(cacheKey);
            if (_memCache.TryGetValue(memCacheKey, out BundleResponse response))
            {
                return response;
            }

            // Disk cache
            response = await _diskCache.GetResponseAsync(cacheKey, bundle);
            if (response != null)
            {
                response = new BundleResponse(response);
                if (response.FileProvider == null)
                {
                    response.FileProvider = bundle.FileProvider ?? _options.CurrentValue.FileProvider;
                }

                PutToMemoryCache(cacheKey, bundle, response);
            }

            return response;
        }

        public async Task PutResponseAsync(BundleCacheKey cacheKey, Bundle bundle, BundleResponse response)
        {
            Guard.NotNull(cacheKey.Key, nameof(cacheKey.Key));
            Guard.NotNull(bundle, nameof(bundle));
            Guard.NotNull(response, nameof(response));

            response.CacheKey = cacheKey.Key;
            response.CacheKeyFragments = cacheKey.Fragments;

            await _diskCache.PutResponseAsync(cacheKey, bundle, response);
            PutToMemoryCache(cacheKey, bundle, response);
        }

        private void PutToMemoryCache(BundleCacheKey cacheKey, Bundle bundle, BundleResponse response)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                // Expire after 24 h
                .SetSlidingExpiration(TimeSpan.FromHours(24))
                // Do not remove due to memory pressure 
                .SetPriority(CacheItemPriority.NeverRemove);

            foreach (string file in response.IncludedFiles)
            {
                var changeToken = response.FileProvider.Watch(file);
                if (changeToken != null)
                {
                    cacheOptions.AddExpirationToken(changeToken);
                }
            }

            cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                var response = (BundleResponse)value;
                var cacheKey = new BundleCacheKey { Key = response.CacheKey, Fragments = response.CacheKeyFragments };
                _diskCache.RemoveResponseAsync(cacheKey).Await();
            });

            _memCache.Set(BuildScopedCacheKey(cacheKey), response, cacheOptions);
        }

        public async Task RemoveResponseAsync(BundleCacheKey cacheKey)
        {
            await _diskCache.RemoveResponseAsync(cacheKey);
            _memCache.Remove(BuildScopedCacheKey(cacheKey));
        }

        public async Task ClearAsync()
        {
            await _diskCache.ClearAsync();
            _memCache.RemoveByPattern(BuildScopedCacheKey("*"));
        }

        private static string BuildScopedCacheKey(string key)
            => "bundle:" + key;
    }
}
