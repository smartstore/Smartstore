using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Smartstore.Web.Bundling
{
    public interface IBundleCache
    {
        Task<BundleResponse> GetResponseAsync(string cacheKey, Bundle bundle);

        Task PutResponseAsync(string cacheKey, Bundle bundle, BundleResponse response);

        Task RemoveResponseAsync(string cacheKey);

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

        public async Task<BundleResponse> GetResponseAsync(string cacheKey, Bundle bundle)
        {
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

        public async Task PutResponseAsync(string cacheKey, Bundle bundle, BundleResponse response)
        {
            response.CacheKey = cacheKey;
            await _diskCache.PutResponseAsync(cacheKey, bundle, response);
            PutToMemoryCache(cacheKey, bundle, response);
        }

        private void PutToMemoryCache(string cacheKey, Bundle bundle, BundleResponse response)
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
                _diskCache.RemoveResponseAsync(((BundleResponse)value).CacheKey).Await();
            });

            _memCache.Set(BuildScopedCacheKey(cacheKey), response, cacheOptions);
        }

        public async Task RemoveResponseAsync(string cacheKey)
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
