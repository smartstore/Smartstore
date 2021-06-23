using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Smartstore.Web.Assets;
using WebOptimizer;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Hooks JavaScript bundles up to the HTML page.
    /// </summary>
    [HtmlTargetElement("script", Attributes = SrcAttribute)]
    public class ScriptAssetTagHelper : AssetTagHelper
    {
        const string SrcAttribute = "src";
        protected override string SourceAttributeName  => SrcAttribute;

        public ScriptAssetTagHelper(
            IAssetPipeline pipeline,
            IAssetBuilder builder,
            IWebHostEnvironment env,
            IOptionsMonitor<WebOptimizerOptions> options)
            : base(pipeline, builder, env, options)
        {
        }

        protected override IHtmlContent RenderAssetTag(IAsset asset, string path)
        {
            var script = new TagBuilder("script");
            script.Attributes.Add(SourceAttributeName, path);
            return script;
        }
    }

    /// <summary>
    /// Hooks CSS bundles up to the HTML page.
    /// </summary>
    [HtmlTargetElement("link", Attributes = "href, [rel=stylesheet]")]
    [HtmlTargetElement("link", Attributes = "href, [rel=preload]")]
    [HtmlTargetElement("link", Attributes = "href, [rel=prefetch]")]
    public class LinkAssetTagHelper : AssetTagHelper
    {
        const string HrefAttribute = "href";
        protected override string SourceAttributeName => HrefAttribute;

        public LinkAssetTagHelper(
            IAssetPipeline pipeline,
            IAssetBuilder builder,
            IWebHostEnvironment env,
            IOptionsMonitor<WebOptimizerOptions> options)
            : base(pipeline, builder, env, options)
        {
        }

        protected override IHtmlContent RenderAssetTag(IAsset asset, string path)
        {
            var script = new TagBuilder("link");
            script.Attributes.Add("rel", "stylesheet");
            script.Attributes.Add(SourceAttributeName, path);
            return script;
        }
    }

    public abstract class AssetTagHelper : TagHelper
    {
        public AssetTagHelper(
            IAssetPipeline pipeline,
            IAssetBuilder builder,
            IWebHostEnvironment env,
            IOptionsMonitor<WebOptimizerOptions> options)
        {
            AssetPipeline = pipeline;
            AssetBuilder = builder;
            HostEnvironment = env;
            Options = options.CurrentValue;
        }

        /// <summary>
        /// Makes sure this TagHelper runs before the built in ones.
        /// </summary>
        public override int Order => 10;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        protected IWebHostEnvironment HostEnvironment { get; }

        protected IAssetPipeline AssetPipeline { get; }

        protected IAssetBuilder AssetBuilder { get; }

        protected IWebOptimizerOptions Options { get; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var src = GetSourceValue(output);
            if (src.IsEmpty())
            {
                return;
            }

            string pathBase = ViewContext.HttpContext.Request.PathBase.Value.NullEmpty();
            if (pathBase != null && src.StartsWith(pathBase))
            {
                src = src[pathBase.Length..];
            }

            if (AssetPipeline.TryGetAssetFromRoute(src, out var asset))
            {
                if (Options.EnableTagHelperBundling == true)
                {
                    src = $"{pathBase}{asset.Route}";

                    var response = await AssetBuilder.BuildAsync(asset, ViewContext.HttpContext, Options);
                    if (response is SmartAssetResponse smartResponse && smartResponse.ContentHash.HasValue())
                    {
                        src += "?v=" + smartResponse.ContentHash;
                    }

                    output.Attributes.SetAttribute(SourceAttributeName, src);
                }
                else
                {
                    output.SuppressOutput();

                    var sourceFiles = asset.ExpandGlobPatterns(HostEnvironment);

                    foreach (var file in sourceFiles)
                    {
                        output.PostElement.AppendHtml(RenderAssetTag(asset, file));
                        output.PostElement.AppendLine();
                    }
                }
            }
        }

        protected abstract IHtmlContent RenderAssetTag(IAsset asset, string path);

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
