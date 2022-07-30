using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Smartstore.Threading;

namespace Smartstore.Web.Bundling
{
    internal class BundleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IBundleCollection _bundles;
        private readonly IBundleCache _bundleCache;
        private readonly IBundleBuilder _bundleBuilder;
        private readonly IOptionsMonitor<BundlingOptions> _optionsMonitor;
        private readonly ILogger _logger;

        public BundleMiddleware(
            RequestDelegate next,
            IBundleCollection bundles,
            IBundleCache bundleCache,
            IBundleBuilder bundleBuilder,
            IOptionsMonitor<BundlingOptions> optionsMonitor,
            ILogger<BundleMiddleware> logger)
        {
            _next = next;
            _bundles = bundles;
            _bundleCache = bundleCache;
            _bundleBuilder = bundleBuilder;
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var path = httpContext.Request.Path;
            if (!path.HasValue)
            {
                await _next(httpContext);
                return;
            }

            var bundle = _bundles.GetBundleFor(path);
            if (bundle == null)
            {
                await _next(httpContext);
                return;
            }

            _logger.Debug("Request for bundle '{0}' started.", bundle.Route);

            var cacheKey = bundle.GetCacheKey(httpContext);
            var options = _optionsMonitor.CurrentValue;

            var bundleResponse = await _bundleCache.GetResponseAsync(cacheKey, bundle);
            if (bundleResponse != null)
            {
                _logger.Debug("Serving bundle '{0}' from cache.", bundle.Route);
                await ServeBundleResponse(bundleResponse, httpContext, options);
                return;
            }

            using (await AsyncLock.KeyedAsync("bm_" + cacheKey))
            {
                // Double paranoia check
                bundleResponse = await _bundleCache.GetResponseAsync(cacheKey, bundle);

                if (bundleResponse != null)
                {
                    _logger.Debug("Serving bundle '{0}' from cache.", bundle.Route);
                    await ServeBundleResponse(bundleResponse, httpContext, options);
                    return;
                }

                try
                {
                    // Build
                    bundleResponse = await _bundleBuilder.BuildBundleAsync(bundle, cacheKey, null, httpContext, options);

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
                catch (Exception ex)
                {
                    await ServerErrorResponse(ex, bundle, httpContext);
                    _logger.Error(ex, $"Error while processing bundle '{bundle.Route}'.");
                }
            }
        }

        private static ValueTask ServeBundleResponse(BundleResponse bundleResponse, HttpContext httpContext, BundlingOptions options, bool noCache = false)
        {
            var response = httpContext.Response;
            var contentHash = bundleResponse.ContentHash;

            response.ContentType = bundleResponse.ContentType;

            if (!noCache)
            {
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
            }

            if (bundleResponse.Content?.Length > 0)
            {
                SetCompressionMode(httpContext, options);
                var buffer = bundleResponse.Content.GetBytes();
                return response.Body.WriteAsync(buffer.AsMemory(0, buffer.Length));
            }

            return ValueTask.CompletedTask;
        }

        private static ValueTask ServerErrorResponse(Exception ex, Bundle bundle, HttpContext httpContext)
        {
            var response = httpContext.Response;
            response.ContentType = bundle.ContentType;
            response.StatusCode = 500;

            var content = $"/*\n{ex.ToAllMessages()}\n*/";
            var buffer = content.GetBytes();
            return response.Body.WriteAsync(buffer.AsMemory(0, buffer.Length));
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
