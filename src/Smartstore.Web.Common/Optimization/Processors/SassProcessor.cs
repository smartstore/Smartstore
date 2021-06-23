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
using Smartstore.IO;
using Smartstore.Engine;

namespace Smartstore.Web.Optimization.Processors
{
    public class SassProcessor : IProcessor
    {
        private readonly WebOptimazerScssOptions _options;

        public SassProcessor(WebOptimazerScssOptions options = null)
        {
            _options = options;
        }

        public Task ExecuteAsync(IAssetContext context)
        {
            var content = new Dictionary<string, byte[]>();
            var env = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
            var fileProvider = context.Asset.GetFileProvider(env);

            foreach (string route in context.Content.Keys)
            {
                //var file = fileProvider.GetFileInfo(route);
                var fs = context.HttpContext.RequestServices.GetRequiredService<IApplicationContext>().WebRoot;
                var settings = new ScssOptions { InputFile = route };
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
                    return OnTryImportSassFile(fs, ref file, parentPath, out scss, out map);
                };

                var watch = Stopwatch.StartNew();
                var result = Scss.ConvertToCss(context.Content[route].AsString(), settings);
                watch.Stop();
                Debug.WriteLine($"LibSass time for {route}: {watch.ElapsedMilliseconds} ms.");

                context.Asset.SetIncludedFiles(result.IncludedFiles?.Concat(new[] { route }));

                content[route] = result.Css.AsByteArray();
            }

            context.Content = content;

            return Task.CompletedTask;
        }

        public string CacheKey(HttpContext context)
            => GenerateCacheKey(context);

        private string GenerateCacheKey(HttpContext context)
        {
            // TODO: (core) Vary by theme and store.
            return string.Empty;
        }

        private static bool OnTryImportSassFile(IFileSystem fs, ref string file, string parentPath, out string scss, out string map)
        {
            map = null;
            scss = null;

            if (!file.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
            {
                file += ".scss";
            }

            var parentDir = fs.GetDirectoryForFile(parentPath);
            var importFile = fs.GetFile(fs.PathCombine(parentDir.SubPath, file));
            if (!importFile.Exists && file[0] != '_')
            {
                if (file[0] == '_')
                {
                    return false;
                }
                else
                {
                    var slashIndex = file.LastIndexOf('/');
                    if (slashIndex > -1)
                    {
                        file = file.Substring(0, slashIndex + 1) + "_" + file.Substring(slashIndex + 1);
                    }
                    else
                    {
                        file = "_" + file;
                    }

                    importFile = fs.GetFile(fs.PathCombine(parentDir.SubPath, file));
                    if (!importFile.Exists)
                    {
                        return false;
                    }
                }
            }

            file = importFile.SubPath;
            scss = importFile.ReadAllText();

            return true;
        }
    }
}
