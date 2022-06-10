using Smartstore.Scheduling;

namespace Smartstore.Caching.Tasks
{
    public class ClearCacheTask : ITask
    {
        private readonly ICacheManager _cacheManager;

        public ClearCacheTask(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            return _cacheManager.ClearAsync();
        }
    }
}
