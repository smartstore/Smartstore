using DouglasCrockford.JsMin;

namespace Smartstore.Web.Bundling.Processors
{
    public class JsMinifyProcessor : BundleProcessor
    {
        internal static string JsContentType = "application/javascript";
        internal static readonly JsMinifyProcessor Instance = new();
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
                    var minResult = MinifyCore(asset, context);
                    asset.Content = minResult;
                    context.ProcessorCodes.Add(Code);
                }
                catch (Exception ex)
                {
                    var nl = Environment.NewLine;
                    asset.Content = "/* " + nl + ex.ToAllMessages() + " */" + nl + asset.Content;
                }
            }

            return Task.CompletedTask;
        }

        protected virtual string MinifyCore(AssetContent asset, BundleContext context)
        {
            return Minifier.Minify(asset.Content);
        }
    }
}
