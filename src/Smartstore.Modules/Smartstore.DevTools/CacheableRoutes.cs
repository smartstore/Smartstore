using Smartstore.Core.OutputCache;

namespace Smartstore.DevTools
{
    internal sealed class CacheableRoutes : ICacheableRouteProvider
    {
        public int Order => 0;

        public IEnumerable<string> GetCacheableRoutes()
            => ["vc:Smartstore.DevTools/CustomFacet"];
    }
}
