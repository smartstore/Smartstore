using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Smartstore.Web.Bundling;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Hooks JavaScript bundles up to the HTML page.
    /// </summary>
    [HtmlTargetElement("script", Attributes = SrcAttribute)]
    public class ScriptBundleTagHelper : BundleTagHelper
    {
        const string SrcAttribute = "src";
        protected override string SourceAttributeName  => SrcAttribute;

        public ScriptBundleTagHelper(
            IBundleCollection bundles,
            IBundleCache bundleCache,
            IOptionsMonitor<BundlingOptions> options,
            IWebHostEnvironment env)
            : base(bundles, bundleCache, options, env)
        {
        }

        protected override IHtmlContent RenderAssetTag(BundleFile file)
        {
            var script = new TagBuilder("script");
            script.Attributes.Add(SourceAttributeName, file.Path);
            return script;
        }
    }

    /// <summary>
    /// Hooks CSS bundles up to the HTML page.
    /// </summary>
    [HtmlTargetElement("link", Attributes = "href, [rel=stylesheet]")]
    [HtmlTargetElement("link", Attributes = "href, [rel=preload]")]
    [HtmlTargetElement("link", Attributes = "href, [rel=prefetch]")]
    public class StyleBundleTagHelper : BundleTagHelper
    {
        const string HrefAttribute = "href";
        protected override string SourceAttributeName => HrefAttribute;

        public StyleBundleTagHelper(
            IBundleCollection bundles,
            IBundleCache bundleCache,
            IOptionsMonitor<BundlingOptions> options,
            IWebHostEnvironment env)
            : base(bundles, bundleCache, options, env)
        {
        }

        protected override IHtmlContent RenderAssetTag(BundleFile file)
        {
            var script = new TagBuilder("link");
            script.Attributes.Add("rel", "stylesheet");
            script.Attributes.Add(SourceAttributeName, file.Path);
            return script;
        }
    }

    public abstract class BundleTagHelper : TagHelper
    {
        public BundleTagHelper(
            IBundleCollection bundles,
            IBundleCache bundleCache,
            IOptionsMonitor<BundlingOptions> options,
            IWebHostEnvironment env)
        {
            Bundles = bundles;
            BundleCache = bundleCache;
            Options = options.CurrentValue;
            HostEnvironment = env;
        }

        /// <summary>
        /// Makes sure this TagHelper runs before the built in ones.
        /// </summary>
        public override int Order => 10;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        protected IBundleCollection Bundles { get; }

        protected IBundleCache BundleCache { get; }

        protected BundlingOptions Options { get; }

        protected IWebHostEnvironment HostEnvironment { get; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var src = GetSourceValue(output);
            if (src.IsEmpty())
            {
                return;
            }

            var pathBase = ViewContext.HttpContext.Request.PathBase.Value.NullEmpty();
            if (pathBase != null && src.StartsWith(pathBase))
            {
                src = src[pathBase.Length..];
            }

            var bundle = Bundles.GetBundleFor(src);

            if (bundle != null)
            {
                var enableBundling = Options.EnableBundling == true;
                if (!enableBundling && bundle.SourceFiles.Any(x => x.EndsWith(".scss")))
                {
                    // Cannot disable bundling for bundles that contain sass files. 
                    enableBundling = true;
                }

                if (enableBundling)
                {
                    src = $"{pathBase}{bundle.Route}";

                    var cacheKey = bundle.GetCacheKey(ViewContext.HttpContext);
                    var cachedResponse = await BundleCache.GetResponseAsync(cacheKey, bundle);
                    if (cachedResponse != null && cachedResponse.ContentHash.HasValue())
                    {
                        src += "?v=" + cachedResponse.ContentHash;
                    }

                    output.Attributes.SetAttribute(SourceAttributeName, src);
                }
                else
                {
                    output.SuppressOutput();

                    var files = bundle.EnumerateFiles(ViewContext.HttpContext, Options);

                    foreach (var file in files)
                    {
                        output.PostElement.AppendHtml(RenderAssetTag(file));
                        output.PostElement.AppendLine();
                    }
                }
            }
        }

        protected abstract IHtmlContent RenderAssetTag(BundleFile file);

        protected abstract string SourceAttributeName { get; }

        protected string GetSourceValue(TagHelperOutput output)
        {
            if (SourceAttributeName.IsEmpty() || !output.Attributes.TryGetAttribute(SourceAttributeName, out var attr))
            {
                return null;
            }

            if (attr.Value is string str)
            {
                return str;
            }
            else if (attr.Value is IHtmlContent content)
            {
                if (content is HtmlString htmlString)
                {
                    return htmlString.ToString();
                }

                using var writer = new StringWriter();
                content.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }

            return null;
        }

        protected string GetQuote(HtmlAttributeValueStyle style)
        {
            return style switch
            {
                HtmlAttributeValueStyle.DoubleQuotes => "\"",
                HtmlAttributeValueStyle.SingleQuotes => "'",
                _ => string.Empty,
            };
        }
    }
}
