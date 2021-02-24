using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Caching.Tasks
{
    // TODO: (core) Move scheduling infrastructure (except storage) to Smartstore project.
    public class ClearCacheTask //: ITask
    {
        private readonly ICacheManager _cacheManager;

        public ClearCacheTask(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public Task Run(object ctx, CancellationToken cancelToken = default)
        {
            return _cacheManager.ClearAsync();
        }
    }
}
