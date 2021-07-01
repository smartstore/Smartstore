using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Web.Bundling
{
    public interface IBundleDiskCache : IBundleCache
    {
    }

    public class BundleDiskCache : IBundleDiskCache
    {
        const string DirName = "BundleCache";
        
        private readonly IApplicationContext _appContext;
        private readonly IFileSystem _fs;
        private readonly IOptionsMonitor<BundlingOptions> _options;

        public BundleDiskCache(
            IApplicationContext appContext, 
            IOptionsMonitor<BundlingOptions> options)
        {
            _appContext = appContext;
            _fs = _appContext.TenantRoot;
            _options = options;

            _appContext.TenantRoot.TryCreateDirectory(DirName);
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        //private IDirectory GetCacheDirectory()
        //    => _fs.GetDirectory(DirName);

        private static string ResolveBundleDirectoryName(BundleCacheKey cacheKey)
        {
            return PathUtility.SanitizeFileName(cacheKey.Key.Trim('/', '\\'));
        }

        public async Task<BundleResponse> GetResponseAsync(BundleCacheKey cacheKey, Bundle bundle)
        {
            if (_options.CurrentValue.EnableDiskCache == false)
            {
                return null;
            }
            
            Guard.NotNull(cacheKey.Key, nameof(cacheKey.Key));
            Guard.NotNull(bundle, nameof(bundle));

            var subpath = _fs.PathCombine(DirName, ResolveBundleDirectoryName(cacheKey));
            
            if (_fs.DirectoryExists(subpath))
            {
                // ...
            }

            await Task.Delay(0);

            return null;
        }

        public async Task PutResponseAsync(BundleCacheKey cacheKey, Bundle bundle, BundleResponse response)
        {
            if (_options.CurrentValue.EnableDiskCache == false)
            {
                return;
            }

            Guard.NotNull(cacheKey.Key, nameof(cacheKey.Key));
            Guard.NotNull(bundle, nameof(bundle));
            Guard.NotNull(response, nameof(response));

            await Task.Delay(0);
        }

        public Task RemoveResponseAsync(BundleCacheKey cacheKey)
        {
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }
    }
}
