using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Smartstore.Http;

namespace Smartstore.Web.Bundling
{
    public class BundleTagGenerator : IAssetTagGenerator
    {
        const string ScriptTemplate = "<script src=\"{0}\"></script>";
        const string StylesheetTemplate = "<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />";

        private readonly IBundleCollection _bundles;
        private readonly IBundleCache _bundleCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptionsMonitor<BundlingOptions> _options;

        public BundleTagGenerator(
            IBundleCollection bundles,
            IBundleCache bundleCache,
            IHttpContextAccessor httpContextAccessor,
            IOptionsMonitor<BundlingOptions> options)
        {
            _bundles = bundles;
            _bundleCache = bundleCache;
            _httpContextAccessor = httpContextAccessor;
            _options = options;
        }

        public IHtmlContent GenerateScript(string url)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && TryGetBundle(url, out var bundle))
            {
                return GenerateTag(bundle, httpContext, ScriptTemplate, _options.CurrentValue.EnableBundling == true);
            }

            return null;
        }

        public IHtmlContent GenerateStylesheet(string url)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && TryGetBundle(url, out var bundle))
            {
                var enableBundling = _options.CurrentValue.EnableBundling == true;
                if (!enableBundling && bundle.SourceFiles.Any(x => x.EndsWith(".scss")))
                {
                    // Cannot disable bundling for bundles that contain sass files. 
                    enableBundling = true;
                }

                return GenerateTag(bundle, httpContext, StylesheetTemplate, enableBundling);
            }

            return null;
        }

        private IHtmlContent GenerateTag(Bundle bundle, HttpContext httpContext, string tagTemplate, bool enableBundling)
        {
            if (enableBundling)
            {
                var url = ResolveBundleUrl(bundle, httpContext);
                return new HtmlString(tagTemplate.FormatInvariant(url));
            }
            else
            {
                var builder = new SmartHtmlContentBuilder();

                var files = bundle.EnumerateFiles(httpContext, _options.CurrentValue);

                foreach (var file in files)
                {
                    builder.AppendHtml(tagTemplate.FormatInvariant(httpContext.Request.PathBase + file.Path));
                    builder.AppendLine();
                }

                return builder;
            }
        }

        private bool TryGetBundle(string url, out Bundle bundle)
        {
            bundle = null;

            if (url.IsEmpty())
            {
                return false;
            }

            if (!WebHelper.IsLocalUrl(url))
            {
                return false;
            }

            if (WebHelper.IsAbsolutePath(url, out var relativePath))
            {
                url = relativePath.Value;
            }

            bundle = _bundles.GetBundleFor(url);
            return bundle != null;
        }

        private string ResolveBundleUrl(Bundle bundle, HttpContext httpContext)
        {
            var url = $"{httpContext.Request.PathBase}{bundle.Route}";

            var cacheKey = bundle.GetCacheKey(httpContext);
            var cachedResponse = _bundleCache.GetResponseAsync(cacheKey, bundle).Await();
            if (cachedResponse != null && cachedResponse.ContentHash.HasValue())
            {
                url += "?v=" + cachedResponse.ContentHash;
            }

            return url;
        }
    }
}
