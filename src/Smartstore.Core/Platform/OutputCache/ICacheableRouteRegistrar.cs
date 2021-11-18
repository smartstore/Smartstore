using System.Threading.Tasks;

namespace Smartstore.Core.OutputCache
{
    public interface ICacheableRouteRegistrar
    {
        Task RegisterCacheableRouteAsync(params string[] routes);
        Task RemoveCacheableRouteAsync(params string[] routes);
    }

    public class NullCacheableRouteRegistrar : ICacheableRouteRegistrar
    {
        public Task RegisterCacheableRouteAsync(params string[] routes) => Task.CompletedTask;
        public Task RemoveCacheableRouteAsync(params string[] routes) => Task.CompletedTask;
    }
}
