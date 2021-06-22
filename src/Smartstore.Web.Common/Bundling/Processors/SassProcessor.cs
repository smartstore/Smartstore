using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using SharpScss;
using WebOptimizer;
using WebOptimizer.Sass;
using System.Diagnostics;

namespace Smartstore.Web.Bundling.Processors
{
    /// <summary>
    /// Compiles Sass files
    /// </summary>
    public class SassProcessor : IProcessor
    {
        private readonly WebOptimazerScssOptions _options;

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public SassProcessor(WebOptimazerScssOptions options = null)
        {
            _options = options;
        }

        /// <summary>
        /// Executes the processor on the specified configuration.
        /// </summary>
        public Task ExecuteAsync(IAssetContext context)
        {
            var content = new Dictionary<string, byte[]>();
            var env = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
            var fileProvider = context.Asset.GetFileProvider(env);

            foreach (string route in context.Content.Keys)
            {
                var file = fileProvider.GetFileInfo(route);
                var settings = new ScssOptions { InputFile = file.PhysicalPath };
                if (_options != null)
                {
                    settings.IncludePaths.AddRange(_options.IncludePaths);
                    settings.GenerateSourceMap = _options.GenerateSourceMap;
                    settings.Indent = _options.Indent;
                    settings.IsIndentedSyntaxSource = _options.IsIndentedSyntaxSource;
                    settings.Linefeed = _options.Linefeed;
                    settings.OmitSourceMapUrl = _options.OmitSourceMapUrl;
                    settings.SourceComments = _options.SourceComments;
                    settings.SourceMapContents = _options.SourceMapContents;
                    settings.SourceMapEmbed = _options.SourceMapEmbed;
                    settings.SourceMapRoot = _options.SourceMapRoot;
                    settings.TryImport = _options.TryImport;
                }

                settings.TryImport ??= (ref string file, string parentPath, out string scss, out string map) =>
                {
                    scss = string.Empty;
                    map = null;

                    return true;
                };

                var watch = Stopwatch.StartNew();
                var result = Scss.ConvertToCss(context.Content[route].AsString(), settings);
                watch.Stop();
                Debug.WriteLine($"LibSass time for {route}: {watch.ElapsedMilliseconds} ms.");

                if (context.Asset is SmartAsset smartAsset)
                {
                    smartAsset.IncludedFiles = result.IncludedFiles;
                }

                content[route] = result.Css.AsByteArray();
            }

            context.Content = content;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public string CacheKey(HttpContext context)
            => GenerateCacheKey(context);

        private string GenerateCacheKey(HttpContext context)
        {
            // TODO: (core) Vary by theme and store.
            return string.Empty;
        }
    }
}
