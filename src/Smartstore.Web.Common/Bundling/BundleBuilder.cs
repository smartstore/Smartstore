using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Smartstore.Diagnostics;

namespace Smartstore.Web.Bundling
{
    /// <summary>
    /// Responsible for building bundles.
    /// </summary>
    public interface IBundleBuilder
    {
        /// <summary>
        /// Builds the given <paramref name="bundle"/> by loading included files into memory and passing them through the processing pipeline.
        /// </summary>
        /// <param name="bundle">The bundle to generate content for.</param>
        /// <param name="cacheKey">The cache key of the current bundle request.</param>
        /// <param name="dataTokens">Optional items to pass through the processing pipeline.</param>
        /// <param name="httpContext"><see cref="HttpContext"/> instance or <c>null</c> to auto-resolve.</param>
        /// <param name="options"><see cref="BundlingOptions"/> instance or <c>null</c> to auto-resolve.</param>
        /// <returns>A <see cref="BundleResponse"/> instance.</returns>
        Task<BundleResponse> BuildBundleAsync(
            Bundle bundle,
            BundleCacheKey cacheKey,
            IDictionary<string, object> dataTokens = null,
            HttpContext httpContext = null,
            BundlingOptions options = null);
    }

    public class DefaultBundleBuilder : IBundleBuilder
    {
        private readonly IBundleContextAccessor _bundleContextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptionsMonitor<BundlingOptions> _bundlingOptions;
        private readonly IChronometer _chronometer;

        public DefaultBundleBuilder(
            IBundleContextAccessor bundleContextAccessor,
            IHttpContextAccessor httpContextAccessor,
            IOptionsMonitor<BundlingOptions> bundlingOptions,
            IChronometer chronometer)
        {
            _bundleContextAccessor = bundleContextAccessor;
            _httpContextAccessor = httpContextAccessor;
            _bundlingOptions = bundlingOptions;
            _chronometer = chronometer;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<BundleResponse> BuildBundleAsync(
            Bundle bundle,
            BundleCacheKey cacheKey,
            IDictionary<string, object> dataTokens = null,
            HttpContext httpContext = null,
            BundlingOptions options = null)
        {
            options ??= _bundlingOptions.CurrentValue;
            httpContext ??= _httpContextAccessor.HttpContext;

            Guard.NotNull(bundle, nameof(bundle));
            Guard.NotNull(httpContext, nameof(httpContext));

            Logger.Debug("Building bundle '{0}'.", bundle.Route);

            using var chronometer = _chronometer.Step($"Bundle '{bundle.Route}'");

            var bundleFiles = bundle.EnumerateFiles(httpContext, options)
                .Where(x => x.File.Exists)
                .ToArray();

            if (bundleFiles.Length == 0)
            {
                throw new InvalidOperationException($"The bundle '{bundle.Route}' does not contain any files.");
            }

            var context = new BundleContext
            {
                Bundle = bundle,
                CacheKey = cacheKey,
                HttpContext = httpContext,
                Options = options,
                Files = bundleFiles
            };

            if (dataTokens != null)
            {
                context.DataTokens.Merge(dataTokens);
            }

            _bundleContextAccessor.BundleContext = context;

            foreach (var bundleFile in bundleFiles)
            {
                context.Content.Add(await bundle.LoadContentAsync(bundleFile));
            }

            context.IncludedFiles.AddRange(context.Content.Select(x => x.Path));

            var response = await bundle.GenerateBundleResponseAsync(context);
            _bundleContextAccessor.BundleContext = null;

            return response;
        }
    }
}
