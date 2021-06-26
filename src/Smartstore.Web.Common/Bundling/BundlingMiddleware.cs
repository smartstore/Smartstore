using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smartstore.Web.Theming;
using WebOptimizer;

namespace Smartstore.Web.Bundling
{
    internal class BundlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAssetPipeline _pipeline;
        private readonly ILogger _logger;
        private readonly IAssetBuilder _assetBuilder;
        private readonly IThemeRegistry _themeRegistry;

        public BundlingMiddleware(
            RequestDelegate next, 
            IAssetPipeline pipeline, 
            ILogger<BundlingMiddleware> logger, 
            IAssetBuilder assetBuilder,
            IThemeRegistry themeRegistry)
        {
            _next = next;
            _pipeline = pipeline;
            _logger = logger;
            _assetBuilder = assetBuilder;
            _themeRegistry = themeRegistry;
        }

        public Task InvokeAsync(HttpContext context, IOptions<WebOptimizerOptions> options)
        {
            //var request = context.Request;

            //if (request.PathBase.HasValue)
            //{
            //    var pathBase = request.PathBase.Value;
            //    if (route.StartsWith(pathBase))
            //    {
            //        route = route[pathBase.Length..];
            //    }
            //}

            if (TryGetAsset(context.Request.Path, out var asset))
            {
                _logger.Debug("Request for asset '{0}' started.", context.Request.Path);
                return HandleAssetAsync(context, asset, options.Value);
            }

            return _next(context);
        }

        private bool TryGetAsset(PathString path, out IAsset asset)
        {
            var route = path.Value;

            if (_pipeline.TryGetAssetFromRoute(route, out asset))
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

        private async Task HandleAssetAsync(HttpContext context, IAsset asset, WebOptimizerOptions options)
        {
            var assetResponse = await _assetBuilder.BuildAsync(asset, context, options);

            if (assetResponse == null)
            {
                await _next(context);
                return;
            }

            await WriteOutputAsync(context, asset, assetResponse, assetResponse.CacheKey, options);
        }

        private async Task WriteOutputAsync(HttpContext context, IAsset asset, IAssetResponse cachedResponse, string cacheKey, WebOptimizerOptions options)
        {
            var response = context.Response;
            var contentHash = (cachedResponse as SmartAssetResponse)?.ContentHash;

            response.ContentType = asset.ContentType;

            foreach (string name in cachedResponse.Headers.Keys)
            {
                response.Headers[name] = cachedResponse.Headers[name];
            }

            if (cacheKey.HasValue())
            {
                if (options.EnableCaching == true)
                {
                    response.Headers[HeaderNames.CacheControl] = $"max-age=31536000"; // 1 year

                    if (context.Request.Query.ContainsKey("v"))
                    {
                        response.Headers[HeaderNames.CacheControl] += $",immutable";
                    }
                }

                if (contentHash.HasValue())
                {
                    response.Headers[HeaderNames.ETag] = $"\"{contentHash}\"";

                    if (IsConditionalGet(context, contentHash))
                    {
                        response.StatusCode = 304;
                        return;
                    }
                }
            }

            if (cachedResponse?.Body?.Length > 0)
            {
                SetCompressionMode(context, options);
                await response.Body.WriteAsync(cachedResponse.Body.AsMemory(0, cachedResponse.Body.Length));
            }
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

        private static void SetCompressionMode(HttpContext context, IWebOptimizerOptions options)
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
