using NUglify;
using NUglify.Css;

namespace Smartstore.Web.Bundling.Processors
{
    public class NUglifyCssMinProcessor : NUglifyProcessor
    {
        internal static readonly string CssContentType = "text/css";
        internal static readonly NUglifyCssMinProcessor Instance = new();
        internal static readonly CssSettings Settings = new()
        {
            CommentMode = CssComment.None,
            FixIE8Fonts = false,
            ColorNames = CssColor.Strict
        };

        protected internal override UglifyResult MinifyCore(string source)
        {
            return Uglify.Css(source, Settings);
        }
    }
}
