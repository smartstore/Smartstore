using NUglify;
using NUglify.JavaScript;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Web.Bundling.Processors;

public class NUglifyJsMinProcessor : NUglifyProcessor
{
    internal static string JsContentType = "application/javascript";
    internal static readonly NUglifyJsMinProcessor Instance = new();
    internal static readonly CodeSettings Settings = new()
    {
        MinifyCode = true,
        AmdSupport = true,
        ConstStatementsMozilla = false,
        KnownGlobalNamesList = "$,jQuery,_,Smartstore,Res"
    };

    /// <inheritdoc />
    public override void PopulateCacheKey(Bundle bundle, HttpContext httpContext, IDictionary<string, string> values)
    {
        values["jsmin"] = $"nuglify-{typeof(Uglify).Assembly.GetName().Version}";
    }

    protected internal override UglifyResult MinifyCore(string source)
    {
        return Uglify.Js(source, Settings);
    }
}