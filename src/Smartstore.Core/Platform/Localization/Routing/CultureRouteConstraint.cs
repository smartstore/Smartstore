using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Localization.Routing
{
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
                // Don't validate
                return true;
            }
            
            if (!GlobalMatch(httpContext))
            {
                return false;
            }
            
            if (CultureHelper.IsValidCultureCode(cultureCode))
            {
                // INFO: only incoming
                var languageService = httpContext.RequestServices.GetRequiredService<ILanguageService>();
                return languageService.IsPublishedLanguage(cultureCode);

                //var requestCulture = httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture;
                //if (requestCulture == null)
                //{
                //    return false;
                //}
                    
                //return (requestCulture.Name == cultureName || requestCulture.Parent.Name == cultureName);
            }

            return false;
        }

        private bool GlobalMatch(HttpContext httpContext)
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
