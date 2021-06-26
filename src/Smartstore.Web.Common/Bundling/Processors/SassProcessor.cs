using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharpScss;
using Smartstore.Core.Stores;
using Smartstore.IO;
using Smartstore.Web.Theming;
using WebOptimizer;
using WebOptimizer.Sass;

namespace Smartstore.Web.Bundling.Processors
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
            var assetFileProvider = fileProvider as IAssetFileProvider;

            foreach (string route in context.Content.Keys)
            {
                if (!route.EndsWith(".scss"))
                {
                    content[route] = context.Content[route];
                    continue;
                }
                
                var settings = new ScssOptions { InputFile = assetFileProvider != null ? route : fileProvider.GetFileInfo(route).PhysicalPath };
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

                if (assetFileProvider != null)
                {
                    settings.TryImport ??= (ref string file, string parentPath, out string scss, out string map) =>
                    {
                        return OnTryImportSassFile(assetFileProvider, ref file, parentPath, out scss, out map);
                    };
                }

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

        private static string GenerateCacheKey(HttpContext context)
        {
            // TODO: (core) Does not work, because cache key is computed very early during parent request (the path is never themeable)
            var cacheKey = string.Empty;

            if (context.Request.Path.StartsWithSegments("/themes/", StringComparison.OrdinalIgnoreCase, out _)) 
            {
                // It's a themeable request
                var qs = context.Request.Query;
                var services = context.RequestServices;
                var storeContext = services.GetRequiredService<IStoreContext>();
                var themeContext = services.GetRequiredService<IThemeContext>();

                // Required for Theme editing validation: See Admin.Controllers.ThemeController.ValidateSass()
                if (qs.ContainsKey("theme"))
                {
                    themeContext.SetRequestTheme(qs["theme"].ToString());
                }
                if (qs.ContainsKey("storeId"))
                {
                    storeContext.SetRequestStore(qs["storeId"].ToString().ToInt());
                }

                cacheKey += "_" + themeContext.WorkingThemeName + "_" + storeContext.CurrentStore.Id;

                if (context.Request.Query.ContainsKey("validate"))
                {
                    cacheKey += "_Validation";
                }
            }

            return cacheKey;
        }

        private static bool OnTryImportSassFile(IAssetFileProvider fileProvider, ref string file, string parentPath, out string scss, out string map)
        {
            map = null;
            scss = null;

            if (!file.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
            {
                file += ".scss";
            }

            var parentDir = parentPath.Substring(0, Path.GetDirectoryName(parentPath).Length);
            var subPath = fileProvider.PathCombine(parentDir, file);
            var importFile = fileProvider.GetFileInfo(subPath);
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

                    subPath = fileProvider.PathCombine(parentDir, file);
                    importFile = fileProvider.GetFileInfo(subPath);
                    if (!importFile.Exists)
                    {
                        return false;
                    }
                }
            }

            file = subPath;

            using var stream = importFile.CreateReadStream();
            scss = stream.AsString();

            return true;
        }
    }
}
