using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Diagnostics;

namespace Smartstore.Web.Bundling
{
    public interface IBundleBuilder
    {
        Task<BundleResponse> BuildBundleAsync(Bundle bundle, HttpContext httpContext, BundlingOptions options);
    }

    public class DefaultBundleBuilder : IBundleBuilder
    {
        private readonly IChronometer _chronometer;

        public DefaultBundleBuilder(IChronometer chronometer)
        {
            _chronometer = chronometer;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<BundleResponse> BuildBundleAsync(Bundle bundle, HttpContext httpContext, BundlingOptions options)
        {
            Guard.NotNull(bundle, nameof(bundle));
            Guard.NotNull(httpContext, nameof(httpContext));
            Guard.NotNull(options, nameof(options));

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
                HttpContext = httpContext,
                Bundle = bundle,
                Options = options,
                Files = bundleFiles
            };

            foreach (var bundleFile in bundleFiles)
            {
                context.Content.Add(await bundle.LoadContentAsync(bundleFile));
            }

            context.IncludedFiles.AddRange(context.Content.Select(x => x.Path));

            var response = await bundle.GenerateBundleResponseAsync(context);
            return response;
        }
    }
}
