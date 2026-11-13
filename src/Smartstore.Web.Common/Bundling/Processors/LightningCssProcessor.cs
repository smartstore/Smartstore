#nullable enable

using Smartstore.Web.Bundling.LightningCss;

namespace Smartstore.Web.Bundling.Processors;

/// <summary>
/// Minifies, optimizes, and applies browser transformations to a concatenated style bundle.
/// It replaces both CSS minification and autoprefixing in the style pipeline.
/// </summary>
internal sealed class LightningCssProcessor : BundleProcessor
{
    internal static readonly LightningCssProcessor Instance = new();

    private readonly LightningCssTransformer _transformer = new();

    // Successful runs record minification and transpilation separately so the disk cache
    // can validate both options independently.
    public override string Code => BundleProcessorCodes.Transpile;

    public override async Task ProcessAsync(BundleContext context)
    {
        var minify = context.Options.EnableMinification == true;
        var transpile = context.Options.EnableCssTranspilation == true;

        if (!minify && !transpile)
        {
            return;
        }

        // Run after concatenation so optimization and prefixing share one CLI invocation
        // rather than launching a separate process for each source file or operation.
        if (context.Content.Count != 1)
        {
            throw new InvalidOperationException("Lightning CSS requires a single, concatenated CSS asset.");
        }

        var options = LightningCssOptions.Default;

        if (transpile)
        {
            // Browser targets control compatibility transforms; minification alone
            // does not need a target query.
            var targets = context.Options.CssTranspiler.Targets;
            options.Targets = string.IsNullOrWhiteSpace(targets) ? "defaults, not IE 11" : targets;
        }

        options.Minify = minify;
        options.Bundle = false;

        var asset = context.Content[0];
        LightningCssResult result;
        try
        {
            result = await _transformer.TransformAsync(
                asset.Content,
                options,
                context.HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Let the bundle middleware return HTTP 500 instead of caching untransformed CSS.
            // Include the route because concatenation no longer has a meaningful asset path.
            throw new InvalidOperationException($"Lightning CSS failed for bundle '{context.Bundle.Route}': {ex.Message}", ex);
        }

        asset.Content = result.Output;
        asset.IsMinified = minify;

        if (!string.IsNullOrWhiteSpace(result.Diagnostics))
        {
            // Surface non-fatal CLI diagnostics without changing the effective CSS.
            // Escape comment terminators so diagnostics cannot inject CSS rules.
            var message = $"Lightning CSS diagnostics for bundle '{context.Bundle.Route}':{Environment.NewLine}{result.Diagnostics.Trim()}"
                .Replace("*/", "* /");
            asset.Content += $"{Environment.NewLine}/* {message} */";
        }

        if (minify && !context.ProcessorCodes.Contains(BundleProcessorCodes.Minify))
        {
            context.ProcessorCodes.Add(BundleProcessorCodes.Minify);
        }

        if (transpile && !context.ProcessorCodes.Contains(BundleProcessorCodes.Transpile))
        {
            context.ProcessorCodes.Add(BundleProcessorCodes.Transpile);
        }
    }
}
