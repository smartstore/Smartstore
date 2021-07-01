using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Web.Bundling
{
    public interface IBundleProcessor
    {
        void PopulateCacheKey(Bundle bundle, HttpContext httpContext, IDictionary<string, string> values);
        Task ProcessAsync(BundleContext context);
    }

    public abstract class BundleProcessor : IBundleProcessor
    {
        public virtual void PopulateCacheKey(Bundle bundle, HttpContext httpContext, IDictionary<string, string> values)
        {
        }

        public abstract Task ProcessAsync(BundleContext context);
    }
}
