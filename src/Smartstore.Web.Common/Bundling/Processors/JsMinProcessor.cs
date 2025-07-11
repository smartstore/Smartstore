using DouglasCrockford.JsMin;

namespace Smartstore.Web.Bundling.Processors
{
    public class JsMinProcessor : BundleProcessor
    {
        internal static string JsContentType = "application/javascript";
        internal static readonly JsMinProcessor Instance = new();
        private static readonly JsMinifier Minifier = new();

        public override string Code => BundleProcessorCodes.Minify;

        public override Task ProcessAsync(BundleContext context)
        {
            if (context.Options.EnableMinification == false || context.ProcessorCodes.Contains(Code))
            {
                return Task.CompletedTask;
            }

            foreach (var asset in context.Content)
            {
                if (asset.IsMinified)
                {
                    continue;
                }

                try
                {
                    asset.Content = MinifyCore(asset, context);
                    context.ProcessorCodes.Add(Code);
                }
                catch (Exception ex)
                {
                    HandleError(asset, ex.ToAllMessages());
                }
            }

            return Task.CompletedTask;
        }

        protected virtual string MinifyCore(AssetContent asset, BundleContext context)
        {
            return Minifier.Minify(asset.Content);
        }

        private static void HandleError(AssetContent asset, string message)
        {
            asset.Content = "/* JSMIN ERRORS: " + Environment.NewLine + message + " */" + Environment.NewLine + asset.Content;
        }
    }
}
