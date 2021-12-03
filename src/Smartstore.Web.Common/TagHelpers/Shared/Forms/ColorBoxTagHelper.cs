using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("input")]
    [HtmlTargetElement("colorbox", TagStructure = TagStructure.WithoutEndTag)]
    public class ColorBoxTagHelper : BaseFormTagHelper
    {
        const string DefaultColorAttributeName = "sm-default-color";

        /// <summary>
        /// Specifies the default color of the property.
        /// </summary>
        [HtmlAttributeName(DefaultColorAttributeName)]
        public string DefaultColor { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            var value = For?.Model?.ToString() ?? string.Empty;
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

            output.SuppressOutput();

            output.PostElement.AppendHtml(HtmlHelper.ColorBox(name, value, DefaultColor));
        }
    }
}