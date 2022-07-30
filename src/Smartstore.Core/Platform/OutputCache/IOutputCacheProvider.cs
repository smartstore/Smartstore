using Smartstore.Collections;
using Smartstore.Engine.Modularity;
using Smartstore.Threading;

namespace Smartstore.Core.OutputCache
{
    public interface IOutputCacheProvider : IProvider
    {
        /// <summary>
        /// Gets a <see cref="IDistributedLock"/> instance for the given <paramref name="key"/>
        /// used to synchronize access to the underlying cache storage.
        /// </summary>
        IDistributedLock GetLock(string key);

        Task<OutputCacheItem> GetAsync(string key);
        Task SetAsync(string key, OutputCacheItem item);
        Task<bool> ExistsAsync(string key);

        Task RemoveAsync(params string[] keys);
        Task RemoveAllAsync();

        Task<IPagedList<OutputCacheItem>> AllAsync(int pageIndex, int pageSize, bool withContent = false);
        Task<int> CountAsync();

        Task<int> InvalidateByRouteAsync(params string[] routes);
        Task<int> InvalidateByPrefixAsync(string keyPrefix);
        Task<int> InvalidateByTagAsync(params string[] tags);
    }
}
