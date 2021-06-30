using System;
using System.Threading.Tasks;
using DouglasCrockford.JsMin;

namespace Smartstore.Web.Bundling.Processors
{
    public class JsMinifyProcessor : BundleProcessor
    {
        internal static string JsContentType = "text/javascript";
        internal static readonly JsMinifyProcessor Instance = new();
        private static readonly JsMinifier Minifier = new();

        public override Task ProcessAsync(BundleContext context)
        {
            if (context.Options.EnableMinification == false)
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
                    context.ProcessorCodes.Add("min");
                }
                catch (Exception ex)
                {
                    asset.Content = "/* \r\n" + ex.ToAllMessages() + " */\r\n" + asset.Content;
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
