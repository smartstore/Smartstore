using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Diagnostics;
using Smartstore.Utilities;
using WebOptimizer;

namespace Smartstore.Web.Bundling
{
    internal class SmartAssetBuilder : IAssetBuilder
    {
        private readonly IAssetBuilder _inner;
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IChronometer _chronometer;

        public SmartAssetBuilder(IAssetBuilder inner, IMemoryCache cache, IWebHostEnvironment env, IChronometer chronometer)
        {
            _inner = inner;
            _cache = cache;
            _env = env;
            _chronometer = chronometer;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<IAssetResponse> BuildAsync(IAsset asset, HttpContext context, IWebOptimizerOptions options)
        {
            if (asset is not SmartAsset smartAsset)
            {
                return await _inner.BuildAsync(asset, context, options);
            }

            var cacheKey = BuildScopedCacheKey(asset.GenerateCacheKey(context));
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

            byte[] content;
            using (_chronometer.Step($"Bundle '{asset.Route}'"))
            {
                content = await smartAsset.ExecuteAsync(context, options);
            }

            var response = new SmartAssetResponse(content, cacheKey);

            //foreach (var name in context.Response.Headers.Keys)
            //{
            //    response.Headers.Add(name, context.Response.Headers[name]);
            //}

            if (options.AllowEmptyBundle == false && (content == null || content.Length == 0))
            {
                return null;
            }

            AddToMemoryCache(cacheKey, response, smartAsset, options);

            if (options.EnableDiskCache == true)
            {
                // TODO: Disk caching...
            }

            return response;
        }

        private void AddToMemoryCache(string cacheKey, SmartAssetResponse response, SmartAsset asset, IWebOptimizerOptions options)
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

        private static string BuildScopedCacheKey(string key)
            => "asset:" + key;
    }
}
