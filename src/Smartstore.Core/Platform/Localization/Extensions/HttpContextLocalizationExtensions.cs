using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Localization.Routing;

namespace Smartstore.Core.Localization
{
    public static class HttpContextLocalizationExtensions
    {
        /// <summary>
        /// Gets the unvalidated explicit culture code from request path.
        /// </summary>
        /// <returns>The culture / unique SEO code (e.g. 'en', 'de' etc.)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetCultureCode(this HttpContext httpContext)
        {
            return httpContext.GetItem("RequestCultureCode", () => 
            {
                var helper = new LocalizedUrlHelper(httpContext.Request);
                helper.IsLocalizedUrl(out var cultureCode);

                return cultureCode;
            });
        }

        /// <summary>
        /// Gets the unvalidated explicit culture code either from resolved request endpoint or from request path.
        /// </summary>
        /// <param name="localizedRouteMetadata">
        /// An instance of <see cref="LocalizedRouteMetadata"/> if resolved endpoint was a localized route, <c>null</c> otherwise.
        /// This parameter being <c>null</c> indicates that routing was not performed yet OR the culture route constraint rejected
        /// the culture code as invalid.
        /// </param>
        /// <returns>The culture / unique SEO code (e.g. 'en', 'de' etc.)</returns>
        public static string GetCultureCode(this HttpContext httpContext, out LocalizedRouteMetadata localizedRouteMetadata)
        {
            Guard.NotNull(httpContext, nameof(httpContext));

            localizedRouteMetadata = null;

            string cultureCode = null;
            var endpoint = httpContext.GetEndpoint();

            if (endpoint != null)
            {
                // We're running after "UseRouting" middleware. It's safe to resolve from route values.
                cultureCode = httpContext.GetRouteData().Values.GetCultureCode();
                localizedRouteMetadata = endpoint.Metadata.OfType<LocalizedRouteMetadata>().FirstOrDefault();
            }

            if (cultureCode == null)
            {
                // No endpoint culture, 'cause call to this method was probably made before "UseRouting" middleware,
                // or route constraint rejected given culture code.
                // We need to analyze the request path.
                return GetCultureCode(httpContext);
            }

            return cultureCode;
        }
    }
}
