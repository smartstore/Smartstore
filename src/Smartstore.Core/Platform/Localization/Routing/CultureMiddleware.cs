using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Localization.Routing
{
    /// <summary>
    /// Determines and sets working culture and globalization scripts
    /// </summary>
    public class CultureMiddleware
    {
        // DIN 5008.
        private static string[] _deMonthAbbreviations = new[] { "Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", "" };

        private readonly RequestDelegate _next;

        public CultureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IWorkContext workContext, LocalizationSettings localizationSettings)
        {
            var request = context.Request;
            var language = workContext.WorkingLanguage;

            var culture = workContext.CurrentCustomer != null && language != null
                ? new CultureInfo(language.LanguageCulture)
                : new CultureInfo("en-US");

            if (language?.UniqueSeoCode?.EqualsNoCase("de") ?? false)
            {
                culture.DateTimeFormat.AbbreviatedMonthNames = culture.DateTimeFormat.AbbreviatedMonthGenitiveNames = _deMonthAbbreviations;
            }

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // TODO: (core) Write globalization scripts > SetWorkingCultureAttribute.OnActionExecuted

            //// Only GET requests
            //if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            //{
            //    await _next(context);
            //    return;
            //}

            //if (!localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            //{
            //    await _next(context);
            //    return;
            //}

            //// Ensure that current route is localizable
            //var endpoint = context.GetEndpoint();
            //if (!endpoint.Metadata.Any(x => x is ILocalizedRoute))
            //{
            //    await _next(context);
            //    return;
            //}

            await _next(context);
        }
    }
}
