using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Smartstore.Threading;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Bundling
{
    internal class BundleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IBundleCollection _collection;
        private readonly IBundleCache _bundleCache;
        private readonly IBundleBuilder _bundleBuilder;
        private readonly IOptionsMonitor<BundlingOptions> _optionsMonitor;
        private readonly IThemeRegistry _themeRegistry;
        private readonly ILogger _logger;

        public BundleMiddleware(
            RequestDelegate next,
            IBundleCollection collection,
            IBundleCache bundleCache,
            IBundleBuilder bundleBuilder,
            IOptionsMonitor<BundlingOptions> optionsMonitor,
            IThemeRegistry themeRegistry,
            ILogger<BundleMiddleware> logger)
        {
            _next = next;
            _collection = collection;
            _bundleCache = bundleCache;
            _bundleBuilder = bundleBuilder;
            _optionsMonitor = optionsMonitor;
            _themeRegistry = themeRegistry;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (!TryGetBundle(httpContext.Request.Path, out var bundle))
            {
                await _next(httpContext);
                return;
            }

            _logger.Debug("Request for bundle '{0}' started.", bundle.Route);

            var options = _optionsMonitor.CurrentValue;
            var cacheKey = bundle.GetCacheKey(httpContext);
            var bundleResponse = await _bundleCache.GetResponseAsync(cacheKey, bundle);

            if (bundleResponse != null)
            {
                _logger.Debug("Serving bundle '{0}' from cache.", bundle.Route);
                await ServeBundleResponse(bundleResponse, httpContext, options);
                return;
            }

            using (await AsyncLock.KeyedAsync("bm_" + cacheKey))
            {
                bundleResponse =
                    // Double paranoia check
                    await _bundleCache.GetResponseAsync(cacheKey, bundle) ??
                    // Build
                    await _bundleBuilder.BuildBundleAsync(bundle, httpContext, options);

                if (bundleResponse == null)
                {
                    await _next(httpContext);
                    return;
                }

                // Put to cache
                await _bundleCache.PutResponseAsync(cacheKey, bundle, bundleResponse);

                // Serve
                await ServeBundleResponse(bundleResponse, httpContext, options);
            }
        }

        private bool TryGetBundle(PathString path, out Bundle bundle)
        {
            var route = path.Value;

            bundle = _collection.GetBundleFor(route);

            if (bundle != null)
            {
                return true;
            }

            //// TODO: (core) Complete dynamic registration for theme sass files
            //if (path.StartsWithSegments("themes/", StringComparison.OrdinalIgnoreCase, out var remaining))
            //{
            //    var segments = remaining.Value.Trim('/').Tokenize('/').ToArray();
            //    if (segments.Length > 1)
            //    {
            //        route = segments[1];
            //        if (_pipeline.TryGetAssetFromRoute(route, out asset))
            //        {
            //            return true;
            //        }

            //        var themeName = segments[0];
            //        var theme = _themeRegistry.GetThemeManifest(themeName);
            //        if (theme != null)
            //        {
            //            asset = _pipeline.RegisterCssBundle("/themes/flex.css", $"/Themes/{themeName}/theme.scss");
            //        }
            //    }
            //}

            return false;
        }

        private static ValueTask ServeBundleResponse(BundleResponse bundleResponse, HttpContext httpContext, BundlingOptions options)
        {
            var response = httpContext.Response;
            var contentHash = bundleResponse.ContentHash;

            response.ContentType = bundleResponse.ContentType;

            if (options.EnableClientCache == true)
            {
                response.Headers[HeaderNames.CacheControl] = "max-age=31536000"; // 1 year

                if (httpContext.Request.Query.ContainsKey("v"))
                {
                    response.Headers[HeaderNames.CacheControl] += ",immutable";
                }
            }

            if (contentHash.HasValue())
            {
                response.Headers[HeaderNames.ETag] = $"\"{contentHash}\"";

                if (IsConditionalGet(httpContext, contentHash))
                {
                    response.StatusCode = 304;
                    return ValueTask.CompletedTask;
                }
            }

            if (bundleResponse.Content?.Length > 0)
            {
                SetCompressionMode(httpContext, options);
                var buffer = Encoding.UTF8.GetBytes(bundleResponse.Content);
                return response.Body.WriteAsync(buffer.AsMemory(0, buffer.Length));
            }

            return ValueTask.CompletedTask;
        }

        private static bool IsConditionalGet(HttpContext context, string contentHash)
        {
            var headers = context.Request.Headers;

            if (headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch))
            {
                return contentHash == ifNoneMatch.ToString().Trim('"');
            }

            if (headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedSince))
            {
                if (context.Response.Headers.TryGetValue(HeaderNames.LastModified, out var lastModified))
                {
                    return ifModifiedSince == lastModified;
                }
            }

            return false;
        }

        private static void SetCompressionMode(HttpContext context, BundlingOptions options)
        {
            // Only called when we expect to serve the body.
            var responseCompressionFeature = context.Features.Get<IHttpsCompressionFeature>();
            if (responseCompressionFeature != null)
            {
                responseCompressionFeature.Mode = options.HttpsCompression;
            }
        }
    }
}
