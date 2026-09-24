using Microsoft.AspNetCore.Http;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.Web.Sass;

namespace Smartstore.Web.Bundling.Processors;

public class SassProcessor : BundleProcessor
{
    internal static readonly SassProcessor Instance = new();

    public override async Task ProcessAsync(BundleContext context)
    {
        var sassAssets = context.Content
            .Where(x => x.Path.EndsWith(".scss", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (sassAssets.Length == 0)
        {
            return;
        }

        await using var lease = await context.SassCompilerFactory.GetCompilerAsync();
        foreach (var asset in sassAssets)
        {
            var result = await lease.Value.CompileAsync(new SassCompilationRequest
            {
                Source = asset.Content,
                SourcePath = asset.Path,
                FileProvider = asset.FileProvider,
                Minify = context.Options.EnableMinification == true
            });

            context.IncludedFiles.AddRange(result.IncludedFiles);

            asset.Content = result.Css;
            asset.IsMinified = context.Options.EnableMinification == true;
        }
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
}
