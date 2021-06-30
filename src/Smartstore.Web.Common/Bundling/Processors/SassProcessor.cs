using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharpScss;
using Smartstore.Core.Stores;
using Smartstore.IO;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Bundling.Processors
{
    public class SassProcessor : BundleProcessor
    {
        internal static readonly SassProcessor Instance = new();

        public override Task ProcessAsync(BundleContext context)
        {
            foreach (var asset in context.Content)
            {
                if (!asset.Path.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileProvider = asset.FileProvider;
                var assetFileProvider = fileProvider as IBundleFileProvider;
                var sassOptions = new ScssOptions
                {
                    InputFile = assetFileProvider != null ? asset.Path : fileProvider.GetFileInfo(asset.Path).PhysicalPath,
                    OutputStyle = context.Options.EnableMinification == true ? ScssOutputStyle.Compressed : ScssOutputStyle.Nested
                };

                if (assetFileProvider != null)
                {
                    sassOptions.TryImport ??= (ref string file, string parentPath, out string scss, out string map) =>
                    {
                        return OnTryImportSassFile(assetFileProvider, ref file, parentPath, out scss, out map);
                    };
                }

                var watch = Stopwatch.StartNew();
                var result = Scss.ConvertToCss(asset.Content, sassOptions);
                watch.Stop();
                Debug.WriteLine($"LibSass time for {asset.Path}: {watch.ElapsedMilliseconds} ms.");

                context.IncludedFiles.AddRange(result.IncludedFiles);

                asset.Content = result.Css;
                asset.IsMinified = context.Options.EnableMinification == true;
            }

            return Task.CompletedTask;
        }

        public override string GetCacheKey(Bundle bundle, HttpContext httpContext, BundlingOptions options)
        {
            // TODO: (core) Does not work, because cache key is computed very early during parent request (the path is never themeable)
            var cacheKey = string.Empty;

            if (bundle.Route.StartsWith("/themes/", StringComparison.OrdinalIgnoreCase))
            {
                // It's a themeable request
                var qs = httpContext.Request.Query;
                var services = httpContext.RequestServices;
                var storeContext = services.GetRequiredService<IStoreContext>();
                var themeContext = services.GetRequiredService<IThemeContext>();

                // Required for theme editing validation: See Admin.Controllers.ThemeController.ValidateSass()
                if (qs.ContainsKey("theme"))
                {
                    themeContext.SetRequestTheme(qs["theme"].ToString());
                }
                if (qs.ContainsKey("storeId"))
                {
                    storeContext.SetRequestStore(qs["storeId"].ToString().ToInt());
                }

                // TODO: (core) Resolve theme name from route, not from themeContext.WorkingThemeName
                cacheKey += "_" + themeContext.WorkingThemeName + "_" + storeContext.CurrentStore.Id;

                if (httpContext.Request.Query.ContainsKey("validate"))
                {
                    cacheKey += "_Validation";
                }
            }

            return cacheKey;
        }

        private static bool OnTryImportSassFile(IBundleFileProvider fileProvider, ref string file, string parentPath, out string scss, out string map)
        {
            map = null;
            scss = null;

            if (!file.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
            {
                file += ".scss";
            }

            var parentDir = parentPath.Substring(0, Path.GetDirectoryName(parentPath).Length);
            var subPath = PathHelper.Combine(parentDir, file);
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

                    subPath = PathHelper.Combine(parentDir, file);
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
