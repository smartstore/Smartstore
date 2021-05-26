using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

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
            var value = string.Empty;
            if (output.Attributes.TryGetAttribute("value", out var valueAttribute))
            {
                value = valueAttribute.Value.ToString();
            }

            var name = string.Empty;
            if (output.Attributes.TryGetAttribute("name", out var nameAttribute))
            {
                name = nameAttribute.Value.ToString();
            }

            var id = string.Empty;
            if (output.Attributes.TryGetAttribute("id", out var idAttribute))
            {
                id = idAttribute.Value.ToString();
            }


            var defaultColor = DefaultColor.EmptyNull();
            var isDefault = value.EqualsNoCase(defaultColor);
            var htmlAttributes = new Dictionary<string, object>
            {
                { "class", "form-control colorval" },
                { "placeholder", defaultColor }
            };

            if (id.HasValue())
            {
                htmlAttributes.Add("id", id);
            }

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.AppendCssClass("input-group colorpicker-component sm-colorbox");
            output.Attributes.Add("data-fallback-color", defaultColor);
            output.Content.AppendHtml(HtmlHelper.TextBox(For != null ? For.Name : name, isDefault ? string.Empty : value, htmlAttributes));

            var addon = new TagBuilder("div");
            addon.AddCssClass("input-group-append input-group-addon");
            addon.InnerHtml.AppendHtml(
                $"<div class='input-group-text'><i class='thecolor' style='{(DefaultColor.HasValue() ? "background-color: " + DefaultColor : string.Empty)}'>&nbsp;</i></div>");

            output.Content.AppendHtml(addon);
        }
    }
}