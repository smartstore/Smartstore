using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Localization.Routing
{
    public class CultureMiddleware
    {
        private readonly RequestDelegate _next;

        public CultureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, LocalizationSettings localizationSettings)
        {
            var culture = CultureInfo.CurrentCulture.Name;
            var uiCulture = CultureInfo.CurrentUICulture.Name;

            var request = context.Request;

            // Only GET requests
            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                await _next(context);
                return;
            }

            // Ensure that current route is localizable
            var endpoint = context.GetEndpoint();
            if (!endpoint.Metadata.Any(x => x is ILocalizedRoute))
            {
                await _next(context);
                return;
            }

            await _next(context);
        }
    }
}
