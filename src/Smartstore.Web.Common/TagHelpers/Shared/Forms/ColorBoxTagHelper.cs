using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("colorbox", Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class ColorBoxTagHelper : BaseFormTagHelper
    {
        const string DefaultColorAttributeName = "sm-default-color";

        [HtmlAttributeName(DefaultColorAttributeName)]
        public string DefaultColor { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            var defaultColor = DefaultColor.EmptyNull();
            var color = For.Model.ToString();
            var isDefault = color.EqualsNoCase(defaultColor);

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.AppendCssClass("input-group colorpicker-component sm-colorbox");
            output.Attributes.Add("data-fallback-color", defaultColor);

            output.Content.AppendHtml(HtmlHelper.TextBox(For.Name, isDefault ? string.Empty : color, new { @class = "form-control colorval", placeholder = defaultColor }));

            var addon = new TagBuilder("div");
            addon.AddCssClass("input-group-append input-group-addon");
            addon.InnerHtml.AppendHtml(
                "<div class=\"input-group-text\"><i class=\"thecolor\" style=\"{0}\">&nbsp;</i></div>".FormatWith(DefaultColor.HasValue() ? "background-color: " + DefaultColor : string.Empty));

            output.Content.AppendHtml(addon);
        }
    }
}