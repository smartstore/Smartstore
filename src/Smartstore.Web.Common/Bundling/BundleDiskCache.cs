using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Web.Bundling
{
    public interface IBundleDiskCache : IBundleCache
    {
    }

    public class BundleDiskCache : IBundleDiskCache
    {
        public Task<BundleResponse> GetResponseAsync(string cacheKey, Bundle bundle)
        {
            return Task.FromResult<BundleResponse>(null);
        }

        public Task PutResponseAsync(string cacheKey, Bundle bundle, BundleResponse response)
        {
            return Task.CompletedTask;
        }

        public Task RemoveResponseAsync(string cacheKey)
        {
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }
    }
}
