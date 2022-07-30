using Microsoft.AspNetCore.Http;
using SharpScss;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.IO;

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
                var assetFileProvider = fileProvider as IAssetFileProvider;
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

                var result = Scss.ConvertToCss(asset.Content, sassOptions);

                context.IncludedFiles.AddRange(result.IncludedFiles);

                asset.Content = result.Css;
                asset.IsMinified = context.Options.EnableMinification == true;

            }

            return Task.CompletedTask;
        }

        public override void PopulateCacheKey(Bundle bundle, HttpContext httpContext, IDictionary<string, string> values)
        {
            if (bundle is not DynamicBundleMatch dynamicBundle || !dynamicBundle.DynamicBundleContext.RouteValues.ContainsKey("theme"))
            {
                return;
            }

            // It's a themeable request
            var qs = httpContext.Request.Query;
            var theme = dynamicBundle.DynamicBundleContext.RouteValues["theme"].Convert<string>();
            var isThemeableRequest = httpContext.Request.Path == dynamicBundle.DynamicBundleContext.Path;
            var services = httpContext.RequestServices;
            var themeContext = services.GetRequiredService<IThemeContext>();
            var storeContext = services.GetRequiredService<IStoreContext>();

            if (isThemeableRequest)
            {
                themeContext.SetRequestTheme(theme);

                if (qs.ContainsKey("storeId"))
                {
                    storeContext.SetRequestStore(qs["storeId"].ToString().ToInt());
                }
            }

            values["Theme"] = theme;
            values["StoreId"] = storeContext.CurrentStore.Id.ToString();
        }

        private static bool OnTryImportSassFile(IAssetFileProvider fileProvider, ref string file, string parentPath, out string scss, out string map)
        {
            map = null;
            scss = null;

            if (!file.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
            {
                file += ".scss";
            }

            var parentDir = parentPath.Substring(0, Path.GetDirectoryName(parentPath).Length + 1);
            var subPath = PathUtility.Combine(parentDir, file);
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

                    subPath = PathUtility.Combine(parentDir, file);
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
