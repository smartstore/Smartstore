using NUglify;
using NUglify.JavaScript;

namespace Smartstore.Web.Bundling.Processors
{
    public class NUglifyJsMinProcessor : NUglifyProcessor
    {
        internal static string JsContentType = "application/javascript";
        internal static readonly NUglifyJsMinProcessor Instance = new();
        internal static readonly CodeSettings Settings = new()
        {
            MinifyCode = true,
            AmdSupport = true,
            KnownGlobalNamesList = "$,jQuery,_,Smartstore"
        };

        protected internal override UglifyResult MinifyCore(string source)
        {
            return Uglify.Js(source, Settings);
        }
    }
}
