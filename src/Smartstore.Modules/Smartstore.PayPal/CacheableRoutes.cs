using Smartstore.Core.OutputCache;

namespace Smartstore.PayPal
{
    internal sealed class CacheableRoutes : ICacheableRouteProvider
    {
        public int Order => 0;

        public IEnumerable<string> GetCacheableRoutes()
        {
            return new string[]
            {
                "vc:Smartstore.PayPal/PayPalPayLater"
            };
        }
    }
}