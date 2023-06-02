using Microsoft.AspNetCore.Http;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Utilities;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Applies the policy for HTTPS according to settings in current <see cref="Smartstore.Core.Stores.Store"/>
    /// </summary>
    public class HttpsUrlFilter : IUrlFilter
    {
        public void Apply(UrlPolicy policy, HttpContext httpContext)
        {
            // Don't redirect in dev environment.
            if (CommonHelper.IsDevEnvironment)
            {
                return;
            }

            // Don't redirect on localhost if not allowed.
            if (httpContext.Connection.IsLocal())
            {
                if (!httpContext.RequestServices.GetService<SecuritySettings>().UseSslOnLocalhost)
                {
                    return;
                }
            }

            // Only redirect for GET requests, otherwise the browser might not propagate
            // the verb and request body correctly.
            if (!httpContext.Request.IsGet())
            {
                return;
            }
            
            var isHttps = httpContext.RequestServices.GetService<IWebHelper>().IsCurrentConnectionSecured();
            var currentStore = httpContext.RequestServices.GetService<IStoreContext>().CurrentStore;
            var supportsHttps = currentStore.SupportsHttps();

            if (supportsHttps && !isHttps)
            {
                var uri = currentStore.GetUri(true);
                policy.Scheme.Modify(uri.Scheme);
                policy.Host.Modify(uri.Authority);
            }
            else if (!supportsHttps && isHttps)
            {
                var uri = currentStore.GetUri(false);
                policy.Scheme.Modify(uri.Scheme);
                policy.Host.Modify(uri.Authority);
            }

            // TBD: Don't redirect from HTTPS --> HTTP (?)
        }
    }
}