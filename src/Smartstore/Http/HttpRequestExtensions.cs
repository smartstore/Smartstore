using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="values">The found values if any</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present, <c>false</c> otherwise.</returns>
        public static bool TryGetValue(this HttpRequest request, string key, out StringValues values)
        {
            values = StringValues.Empty;

            if (request.HasFormContentType)
            {
                values = request.Form[key];
            }

            if (values == StringValues.Empty)
            {
                values = request.Query[key];
            }

            return values != StringValues.Empty;
        }

        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>, and converts value to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="value">The found and converted value</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present and could be converted, <c>false</c> otherwise.</returns>
        public static bool TryGetValueAs<T>(this HttpRequest request, string key, out T value)
        {
            value = default;

            if (request.TryGetValue(key, out var values))
            {
                return ConvertUtility.TryConvert(values.ToString(), out value);
            }

            return false;
        }

        public static bool IsAdminArea(this HttpRequest request)
        {
            Guard.NotNull(request);

            // Try URL prefix
            if (request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Try route
            if (request.HttpContext.TryGetRouteValueAs<string>("area", out var area) && area.EqualsNoCase("admin"))
            {
                // INFO: Module area views can also render in backend. So don't return false if area is not "admin".
                return true;
            }

            return false;
        }

        public static string UserAgent(this HttpRequest httpRequest)
        {
            if (httpRequest.Headers.TryGetValue(HeaderNames.UserAgent, out var value))
            {
                return value.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets the raw request path (PathBase + Path + QueryString)
        /// </summary>
        /// <returns>The raw URL</returns>
        public static string RawUrl(this HttpRequest httpRequest)
        {
            // Try to resolve unencoded raw value from feature.
            var url = httpRequest.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
            if (url.IsEmpty())
            {
                // Fallback
                url = httpRequest.PathBase + httpRequest.Path + httpRequest.QueryString;
            }

            return url;
        }

        public static string UrlReferrer(this HttpRequest httpRequest)
        {
            if (httpRequest.Headers.TryGetValue(HeaderNames.Referer, out var value))
            {
                return value.ToString();
            }

            return null;
        }

        /// <summary>
        /// Checks whether the current request is a GET request.
        /// </summary>
        public static bool IsGet(this HttpRequest httpRequest)
        {
            return HttpMethods.IsGet(httpRequest.Method);
        }

        /// <summary>
        /// Checks whether the current request is a POST request.
        /// </summary>
        public static bool IsPost(this HttpRequest httpRequest)
        {
            return HttpMethods.IsPost(httpRequest.Method);
        }

        /// <summary>
        /// Checks whether the current request is an AJAX request.
        /// </summary>
        public static bool IsAjax(this HttpRequest httpRequest)
        {
            return
                string.Equals(httpRequest.Headers[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal) ||
                string.Equals(httpRequest.Query[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks whether the current request is a regular non-AJAX GET request.
        /// </summary>
        public static bool IsNonAjaxGet(this HttpRequest httpRequest)
        {
            return HttpMethods.IsGet(httpRequest.Method) && !IsAjax(httpRequest);
        }

        /// <summary>
        /// Returns true if the requested resource is one of the typical resources that 
        /// don't need to be processed by the routing system.
        /// </summary>
        /// <returns>True if the request targets a static resource file.</returns>
        /// <remarks>
        /// All known extensions provided by <see cref="FileExtensionContentTypeProvider"/> are considered to be static resources.
        /// </remarks>
        public static bool IsStaticFileRequested(this HttpRequest request)
        {
            if (request is null)
                return false;

            // Requests to .map files may end with a semicolon
            return MimeTypes.TryMapNameToMimeType(request.Path.Value.TrimEnd(';'), out _);
        }
    }
}