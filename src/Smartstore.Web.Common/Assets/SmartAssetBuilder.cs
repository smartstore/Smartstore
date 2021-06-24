using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebOptimizer;

namespace Smartstore.Web.Assets
{
    internal class SmartAssetBuilder : IAssetBuilder
    {
        private readonly IAssetBuilder _inner;
        private IMemoryCache _cache;
        private IWebHostEnvironment _env;

        public SmartAssetBuilder(IAssetBuilder inner, IMemoryCache cache, IWebHostEnvironment env)
        {
            _inner = Guard.NotNull(inner, nameof(inner));
            _cache = Guard.NotNull(cache, nameof(cache));
            _env = Guard.NotNull(env, nameof(env));
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<IAssetResponse> BuildAsync(IAsset asset, HttpContext context, IWebOptimizerOptions options)
        {
            if (asset is not SmartAsset smartAsset)
            {
                return await _inner.BuildAsync(asset, context, options);
            }

            string cacheKey = asset.GenerateCacheKey(context);
            // TODO: Log errors

            if (options.EnableMemoryCache == true && _cache.TryGetValue(cacheKey, out IAssetResponse value))
            {
                Logger.Debug("Serving asset '{0}' from memory cache.", asset.Route);
                return value;
            }
            else if (options.EnableDiskCache == true)
            {
                // TODO: Disk caching...
            }

            var content = await smartAsset.ExecuteAsync(context, options);
            var response = new SmartAssetResponse(content, cacheKey);

            //foreach (var name in context.Response.Headers.Keys)
            //{
            //    response.Headers.Add(name, context.Response.Headers[name]);
            //}

            if (options.AllowEmptyBundle == false && (content == null || content.Length == 0))
            {
                return null;
            }

            AddToCache(cacheKey, response, smartAsset, options);

            if (options.EnableDiskCache == true)
            {
                // TODO: Disk caching...
            }

            return response;
        }

        private void AddToCache(string cacheKey, SmartAssetResponse response, SmartAsset asset, IWebOptimizerOptions options)
        {
            if (options.EnableMemoryCache == true)
            {
                var cacheOptions = new MemoryCacheEntryOptions();
                cacheOptions.SetSlidingExpiration(TimeSpan.FromHours(24));

                var includedFiles = asset.GetIncludedFiles() ?? asset.SourceFiles;
                foreach (string file in includedFiles)
                {
                    cacheOptions.AddExpirationToken(asset.GetFileProvider(_env).Watch(file));
                }

                _cache.Set(cacheKey, response, cacheOptions);
            }
        }
    }
}
