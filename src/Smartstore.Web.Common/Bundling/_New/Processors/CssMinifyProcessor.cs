using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DouglasCrockford.JsMin;
using NUglify;
using NUglify.Css;

namespace Smartstore.Web.Bundling.Processors
{
    public class CssMinifyProcessor : BundleProcessor
    {
        internal static string CssContentType = "text/css";
        internal static readonly CssMinifyProcessor Instance = new();

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

                var settings = new CssSettings
                {
                    CommentMode = CssComment.None,
                    FixIE8Fonts = false,
                    ColorNames = CssColor.Strict
                };

                var result = Uglify.Css(asset.Content, settings);
                var minResult = result.Code;

                if (result.HasErrors)
                {
                    minResult = "/* \r\n" + result.Errors + " */\r\n" + asset.Content;
                }
                else
                {
                    context.ProcessorCodes.Add("min");
                }

                asset.Content = minResult;
            }

            return Task.CompletedTask;
        }
    }
}
