using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Web;

namespace Smartstore
{
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Generates a URL to the local referrer. This method addresses
        /// "Open Redirection Vulnerability" (prevents cross-domain redirects / phishing).
        /// </summary>
        /// <returns>The local referrer URL or <c>null</c> if the referrer is an external host.</returns>
        public static string Referrer(this IUrlHelper url)
        {
            Guard.NotNull(url);

            var httpContext = url.ActionContext.HttpContext;
            var webHelper = httpContext.RequestServices.GetService<IWebHelper>();
            if (webHelper == null)
            {
                return null;
            }

            string referrer = null;
            bool skipLocalCheck = false;
            var requestReferrer = webHelper.GetUrlReferrer();

            if (requestReferrer != null)
            {
                referrer = requestReferrer.OriginalString;
                var domain1 = requestReferrer.GetLeftPart(UriPartial.Authority);
                var domain2 = httpContext.Request.Scheme + Uri.SchemeDelimiter + httpContext.Request.Host;
                if (domain1.EqualsNoCase(domain2))
                {
                    // Always allow fully qualified urls from local host
                    skipLocalCheck = true;
                }
                else
                {
                    referrer = null;
                }
            }

            if (referrer.HasValue())
            {
                // addressing "Open Redirection Vulnerability" (prevent cross-domain redirects / phishing)
                if (!skipLocalCheck && !url.IsLocalUrl(referrer))
                {
                    referrer = null;
                }
                else if (skipLocalCheck)
                {
                    // Just the path & query fragment please
                    referrer = requestReferrer.PathAndQuery;
                }
            }

            return referrer;
        }
    }
}
