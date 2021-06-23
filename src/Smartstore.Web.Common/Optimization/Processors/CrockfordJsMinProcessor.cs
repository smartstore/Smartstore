using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DouglasCrockford.JsMin;
using WebOptimizer;

namespace Smartstore.Web.Optimization.Processors
{
    internal class CrockfordJsMinProcessor : Processor
    {
        public override Task ExecuteAsync(IAssetContext context)
        {
            var minifier = new JsMinifier();
            var dict = new Dictionary<string, byte[]>();

            foreach (string key in context.Content.Keys)
            {
                if (key.EndsWith(".min"))
                {
                    dict[key] = context.Content[key];
                }
                else
                {
                    var source = context.Content[key].AsString();
                    string minResult;

                    try
                    {
                        minResult = minifier.Minify(source);
                        dict[key] = minResult.AsByteArray();
                    }
                    catch (Exception ex)
                    {
                        minResult = "/* \r\n" + ex.ToAllMessages() + " */\r\n" + source;
                    }

                    dict[key] = minResult.AsByteArray();
                }
            }

            context.Content = dict;
            return Task.CompletedTask;
        }
    }
}
