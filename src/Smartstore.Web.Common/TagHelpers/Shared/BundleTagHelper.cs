using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Widgets;

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

        public ScriptBundleTagHelper(IAssetTagGenerator tagGenerator)
            : base(tagGenerator)
        {
        }

        protected override IHtmlContent GenerateTag(string src)
        {
            return TagGenerator.GenerateScript(src);
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

        public StyleBundleTagHelper(IAssetTagGenerator tagGenerator)
            : base(tagGenerator)
        {
        }

        protected override IHtmlContent GenerateTag(string src)
        {
            return TagGenerator.GenerateStylesheet(src);
        }
    }

    public abstract class BundleTagHelper : TagHelper
    {
        public BundleTagHelper(IAssetTagGenerator tagGenerator)
        {
            TagGenerator = tagGenerator;
        }

        /// <summary>
        /// Makes sure this TagHelper runs after UrlResolutionTagHelper but before WidgetTagHelper.
        /// </summary>
        public override int Order => 10;

        protected IAssetTagGenerator TagGenerator { get; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (output.TagName == null)
            {
                // Something went wrong in a previous TagHelper. Get out.
                return;
            }

            var src = GetSourceValue(output);
            var content = GenerateTag(src);

            if (content != null)
            {
                output.SuppressOutput();
                output.PostElement.AppendHtml(content);
            }
        }

        protected abstract IHtmlContent GenerateTag(string src);

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
