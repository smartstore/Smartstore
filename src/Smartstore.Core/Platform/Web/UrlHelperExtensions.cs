#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Web;
using Smartstore.Http;

namespace Smartstore
{
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Generates a URL for the specified action, controller, and route values.
        /// </summary>
        /// <param name="url">The <see cref="IUrlHelper"/> instance used to generate the URL.</param>
        /// <param name="routeInfo">An object containing the action name, controller name, and route values.</param>
        /// <returns>A string representing the generated URL.</returns>
        public static string? Action(this IUrlHelper url, RouteInfo? routeInfo)
        {
            Guard.NotNull(url);
            
            if (routeInfo == null)
            {
                return null;
            }

            return url.Action(routeInfo.Action, routeInfo.Controller, routeInfo.RouteValues);
        }

        /// <summary>
        /// Generates a URL to the local referrer. This method addresses
        /// "Open Redirection Vulnerability" (prevents cross-domain redirects / phishing).
        /// </summary>
        /// <returns>The local referrer URL or <c>null</c> if the referrer is an external host.</returns>
        public static string? Referrer(this IUrlHelper url)
        {
            Guard.NotNull(url);

            var httpContext = url.ActionContext.HttpContext;
            var webHelper = httpContext.RequestServices.GetService<IWebHelper>();
            if (webHelper == null)
            {
                return null;
            }

            string? referrer = null;
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
                    referrer = requestReferrer?.PathAndQuery;
                }
            }

            return referrer;
        }
    }
}
