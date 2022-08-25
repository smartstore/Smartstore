using AutoprefixerHost;
using AutoprefixerHost.Helpers;
using JavaScriptEngineSwitcher.V8;

namespace Smartstore.Web.Bundling.Processors
{
    public class AutoprefixerProcessor : BundleProcessor
    {
        internal static readonly AutoprefixerProcessor Instance = new();

        public override string Code => BundleProcessorCodes.Autoprefix;

        public override Task ProcessAsync(BundleContext context)
        {
            if (context.Options.EnableAutoprefixer == false || context.ProcessorCodes.Contains(Code))
            {
                return Task.CompletedTask;
            }

            var apo = context.Options.Autoprefixer;
            var options = new ProcessingOptions
            {
                Browsers = apo.Browsers?.Count > 0 ? apo.Browsers : new List<string> { "defaults", "not IE 11" },
                Cascade = apo.Cascade,
                Add = apo.Add,
                Remove = apo.Remove,
                Supports = apo.Supports,
                Flexbox = apo.Flexbox ? FlexboxMode.No2009 : FlexboxMode.None,
                Grid = apo.Grid ? GridMode.NoAutoplace : GridMode.None,
                IgnoreUnknownVersions = apo.IgnoreUnknownVersions
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
}
