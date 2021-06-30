using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Web.Bundling
{
    public interface IBundleProcessor
    {
        string GetCacheKey(Bundle bundle, HttpContext httpContext, BundlingOptions options);
        Task ProcessAsync(BundleContext context);
    }

    public abstract class BundleProcessor : IBundleProcessor
    {
        public virtual string GetCacheKey(Bundle bundle, HttpContext httpContext, BundlingOptions options)
            => string.Empty;

        public abstract Task ProcessAsync(BundleContext context);
    }
}
