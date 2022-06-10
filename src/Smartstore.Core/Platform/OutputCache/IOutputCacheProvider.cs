using Smartstore.Collections;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.OutputCache
{
    public interface IOutputCacheProvider : IProvider
    {
        Task<OutputCacheItem> GetAsync(string key);
        Task SetAsync(string key, OutputCacheItem item);
        Task<bool> ExistsAsync(string key);

        Task RemoveAsync(params string[] keys);
        Task RemoveAllAsync();

        Task<PagedList<OutputCacheItem>> AllAsync(int pageIndex, int pageSize, bool withContent = false);
        Task<int> CountAsync();

        Task<int> InvalidateByRouteAsync(params string[] routes);
        Task<int> InvalidateByPrefixAsync(string keyPrefix);
        Task<int> InvalidateByTagAsync(params string[] tags);
    }
}
