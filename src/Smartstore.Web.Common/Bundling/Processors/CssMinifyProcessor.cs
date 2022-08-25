using NUglify;
using NUglify.Css;

namespace Smartstore.Web.Bundling.Processors
{
    public class CssMinifyProcessor : BundleProcessor
    {
        internal static readonly string CssContentType = "text/css";
        internal static readonly CssMinifyProcessor Instance = new();
        private static readonly CssSettings Settings = new()
        {
            CommentMode = CssComment.None,
            FixIE8Fonts = false,
            ColorNames = CssColor.Strict
        };

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

                asset.Content = MinifyCore(asset, context);
            }

            return Task.CompletedTask;
        }

        protected virtual string MinifyCore(AssetContent asset, BundleContext context)
        {
            var result = Uglify.Css(asset.Content, Settings);
            var minResult = result.Code;

            if (result.HasErrors)
            {
                var nl = Environment.NewLine;
                minResult = "/* " + nl + string.Join(nl + nl, result.Errors.Select(x => x.ToString())) + " */" + nl + asset.Content;
            }
            else
            {
                context.ProcessorCodes.Add(Code);
            }

            return minResult;
        }
    }
}
