using AutoprefixerHost;
using AutoprefixerHost.Helpers;
using JavaScriptEngineSwitcher.V8;

namespace Smartstore.Web.Bundling.Processors;

public class AutoprefixerProcessor : BundleProcessor
{
    internal static readonly AutoprefixerProcessor Instance = new();

    public override string Code => BundleProcessorCodes.Autoprefix;

    public override Task ProcessAsync(BundleContext context)
    {
        if (context.Options.EnableCssTranspilation == false || context.ProcessorCodes.Contains(Code))
        {
            return Task.CompletedTask;
        }

        var targets = context.Options.CssTranspiler.Targets;
        // Retained for pipeline comparisons. The old Autoprefixer-specific switches are no
        // longer public settings; these values match the former appsettings defaults.
        var options = new ProcessingOptions
        {
            Browsers = string.IsNullOrWhiteSpace(targets)
                ? ["defaults", "not IE 11"]
                : targets.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            Cascade = false,
            Add = true,
            Remove = true,
            Supports = false,
            Flexbox = FlexboxMode.None,
            Grid = GridMode.None,
            IgnoreUnknownVersions = false
        };

        try
        {
            using (var autoprefixer = new Autoprefixer(new V8JsEngineFactory(), options))
            {
                foreach (var asset in context.Content)
                {
                    try
                    {
                        var result = autoprefixer.Process(asset.Content, context.HttpContext.Request.Path);
                        asset.Content = result.ProcessedContent;
                    }
                    catch (AutoprefixerProcessingException ex)
                    {
                        HandleError(asset, AutoprefixerErrorHelpers.GenerateErrorDetails(ex));
                    }
                    catch (AutoprefixerException ex)
                    {
                        HandleError(asset, AutoprefixerErrorHelpers.GenerateErrorDetails(ex));
                    }
                }
            }
        }
        catch (AutoprefixerLoadException)
        {
            //HandleError(null, AutoprefixerErrorHelpers.GenerateErrorDetails(ex));
            throw;
        }

        return Task.CompletedTask;
    }

    private void HandleError(AssetContent asset, string message)
    {
        var nl = Environment.NewLine;
        var errorHeader = string.Concat(
            "// Autoprefixer error ======================================================================", nl,
            "/*", nl,
            message, nl,
            "*/", nl,
            "// =========================================================================================", nl, nl);

        asset.Content = errorHeader + asset.Content;
    }
}
