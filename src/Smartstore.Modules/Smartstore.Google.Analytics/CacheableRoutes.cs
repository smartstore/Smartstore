using Smartstore.Core.OutputCache;

namespace Smartstore.Google.Analytics
{
    internal sealed class CacheableRoutes : ICacheableRouteProvider
    {
        public int Order => 0;

        public IEnumerable<string> GetCacheableRoutes() => ["vc:Smartstore.Google.Analytics/GoogleAnalytics"];
    }
}
