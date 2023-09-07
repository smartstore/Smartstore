using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("input")]
    [HtmlTargetElement("colorbox", TagStructure = TagStructure.WithoutEndTag)]
    public class ColorBoxTagHelper : BaseFormTagHelper
    {
        const string DefaultColorAttributeName = "sm-default-color";
        const string SwatchesAttributeName = "sm-swatches";

        /// <summary>
        /// Specifies the default color of the property.
        /// </summary>
        [HtmlAttributeName(DefaultColorAttributeName)]
        public string DefaultColor { get; set; }

        /// <summary>
        /// Whether to show color swatches. Default = true.
        /// </summary>
        [HtmlAttributeName(SwatchesAttributeName)]
        public bool Swatches { get; set; } = true;

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            IHtmlContent box;

            if (For != null)
            {
                box = HtmlHelper.ColorBoxFor(For, DefaultColor, Swatches);
            }
            else
            {
                var value = string.Empty;
                if (output.Attributes.TryGetAttribute("value", out var attr))
                {
                    value = attr.ValueAsString();
                }

                var name = string.Empty;
                if (output.Attributes.TryGetAttribute("name", out attr))
                {
                    name = attr.ValueAsString();
                }
                else if (output.Attributes.TryGetAttribute("id", out attr))
                {
                    name = attr.ValueAsString();
                }

                if (name.IsEmpty())
                {
                    throw new InvalidOperationException("The name of a colorbox field cannot be null or empty.");
                }

                box = HtmlHelper.ColorBox(name, value, DefaultColor, Swatches);
            }

            output.SuppressOutput();
            output.PostElement.AppendHtml(box);
        }
    }
}