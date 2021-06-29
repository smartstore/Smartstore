using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DouglasCrockford.JsMin;

namespace Smartstore.Web.Bundling.Processors
{
    public class JsMinifyProcessor : BundleProcessor
    {
        internal static string JsContentType = "text/javascript";
        internal static readonly JsMinifyProcessor Instance = new();

        public override Task ProcessAsync(BundleContext context)
        {
            if (context.Options.EnableMinification == false)
            {
                return Task.CompletedTask;
            }

            var minifier = new JsMinifier();

            foreach (var asset in context.Content)
            {
                if (asset.IsMinified)
                {
                    continue;
                }

                try
                {
                    var minResult = minifier.Minify(asset.Content);
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
    }
}
