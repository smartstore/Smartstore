using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Localization.Routing
{
    [Obsolete("Changed the way localized route matching works")]
    public class CultureRouteConstraint : IRouteConstraint
    {
        const string GlobalMatchKey = "CultureRoutesEnabled";

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            var cultureCode = (string)values.Get(routeKey);

            if (cultureCode.IsEmpty())
            {
                return false;
            }

            if (routeDirection == RouteDirection.UrlGeneration)
            {
                // Don't validate. SmartLinkGenerator will handle url generation.
                return true;
            }

            if (!GlobalMatch(httpContext))
            {
                return false;
            }

            // INFO: Only check for plausibility here. Real validity check will
            // be performed later in the pipeline.
            return CultureHelper.IsValidCultureCode(cultureCode);
        }

        private static bool GlobalMatch(HttpContext httpContext)
        {
            return httpContext.GetItem(GlobalMatchKey, () =>
            {
                var localizationSettings = httpContext.RequestServices.GetRequiredService<LocalizationSettings>();
                if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    return false;
                }

                return true;
            });
        }
    }
}