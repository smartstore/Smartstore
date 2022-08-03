using Microsoft.AspNetCore.Http;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Applies the policy for HTTPS according to settings in current <see cref="Smartstore.Core.Stores.Store"/>
    /// </summary>
    public class HttpsUrlFilter : IUrlFilter
    {
        public void Apply(UrlPolicy policy, HttpContext httpContext)
        {
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

            var currentConnectionSecured = httpContext.RequestServices.GetService<IWebHelper>().IsCurrentConnectionSecured();
            var currentStore = httpContext.RequestServices.GetService<IStoreContext>().CurrentStore;
            var supportsHttps = currentStore.SupportsHttps();
            var requireHttps = currentStore.ForceSslForAllPages;

            if (policy.Endpoint != null && supportsHttps && !requireHttps)
            {
                // Check if RequireSslAttribute is present in endpoint metadata
                requireHttps = policy.Endpoint.Metadata.GetMetadata<RequireSslAttribute>() != null;
            }

            requireHttps = requireHttps && supportsHttps;

            if (requireHttps && !currentConnectionSecured)
            {
                policy.Scheme.Modify(Uri.UriSchemeHttps);
            }
            else if (!requireHttps && currentConnectionSecured)
            {
                policy.Scheme.Modify(Uri.UriSchemeHttp);
            }
        }
    }
}