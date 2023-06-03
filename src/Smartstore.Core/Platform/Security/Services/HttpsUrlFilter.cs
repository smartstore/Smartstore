using Microsoft.AspNetCore.Http;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Utilities;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Applies the policy for HTTPS according to settings in current <see cref="Smartstore.Core.Stores.Store"/>
    /// </summary>
    public class HttpsUrlFilter : IUrlFilter
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public void Apply(UrlPolicy policy, HttpContext httpContext)
        {
            // Only redirect for GET requests, otherwise the browser might not propagate
            // the verb and request body correctly.
            if (!httpContext.Request.IsGet())
            {
                return;
            }

            var isHttps = httpContext.Request.IsHttps;
            var currentStore = httpContext.RequestServices.GetService<IStoreContext>().CurrentStore;
            var supportsHttps = currentStore.SupportsHttps();
            var shouldRedirect = supportsHttps && !isHttps;

            if (!shouldRedirect)
            {
                return;
            }

            // Don't redirect in dev environment.
            if (CommonHelper.IsDevEnvironment)
            {
                Logger.Debug("Redirection to HTTPS suppressed. Reason: Dev environment.");
                return;
            }

            // Don't redirect on localhost if not allowed.
            if (httpContext.Connection.IsLocal())
            {
                if (!httpContext.RequestServices.GetService<SecuritySettings>().UseSslOnLocalhost)
                {
                    Logger.Debug("Redirection to HTTPS suppressed. Reason: Local connection.");
                    return;
                }
            }

            var uri = currentStore.GetBaseUri();
            policy.Scheme.Modify(uri.Scheme);
            policy.Host.Modify(uri.Authority);

            // TBD: Don't redirect from HTTPS to HTTP
        }
    }
}