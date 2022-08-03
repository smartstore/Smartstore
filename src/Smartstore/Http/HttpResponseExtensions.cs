using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Smartstore
{
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Sets the 'Cache-Control' header to 'no-cache, no-store' and 'Pragma' 
        /// header to 'no-cache' overriding any previously set value.
        /// </summary>
        /// <remarks>
        /// This method does NOT check whether response has started already.
        /// </remarks>
        public static void SetDoNotCacheHeaders(this HttpResponse response)
        {
            Guard.NotNull(response, nameof(response));

            var headers = response.Headers;

            if (headers.TryGetValue(HeaderNames.CacheControl, out var cacheControlHeader) &&
                CacheControlHeaderValue.TryParse(cacheControlHeader.ToString(), out var cacheControlHeaderValue))
            {
                // If the Cache-Control is already set, override it only if required
                if (!cacheControlHeaderValue.NoCache || !cacheControlHeaderValue.NoStore)
                {
                    headers.CacheControl = "no-cache, no-store";
                }
            }
            else
            {
                headers.CacheControl = "no-cache, no-store";
            }

            if (headers.TryGetValue(HeaderNames.Pragma, out var pragmaHeader) && pragmaHeader.Count > 0)
            {
                // If the Pragma is already set, override it only if required
                if (!pragmaHeader[0].EqualsNoCase("no-cache"))
                {
                    headers.Pragma = "no-cache";
                }
            }
            else
            {
                headers.Pragma = "no-cache";
            }
        }
    }
}
