using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("input")]
    [HtmlTargetElement("colorbox", TagStructure = TagStructure.WithoutEndTag)]
    public class ColorBoxTagHelper : BaseFormTagHelper
    {
        const string IdAttributeName = "sm-id";
        const string NameAttributeName = "sm-name";
        const string ValueAttributeName = "sm-value";
        const string DefaultColorAttributeName = "sm-default-color";

        /// <summary>
        /// Specifies the id which will be set when rendering the field.
        /// </summary>
        [HtmlAttributeName(IdAttributeName)]
        public string FieldId { get; set; }

        /// <summary>
        /// Specifies the name which will be set if the tag doesn't contain asp-for attribute.
        /// </summary>
        [HtmlAttributeName(NameAttributeName)]
        public string FieldName { get; set; }

        /// <summary>
        /// Specifies the value which will be set when rendering the field.
        /// </summary>
        [HtmlAttributeName(ValueAttributeName)]
        public string FieldValue { get; set; }

        /// <summary>
        /// Specifies the default color of the property.
        /// </summary>
        [HtmlAttributeName(DefaultColorAttributeName)]
        public string DefaultColor { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            string value;
            if (output.Attributes.TryGetAttribute("sm-value", out var valueAttribute))
            {
                value = valueAttribute.Value.ToString();
            }
            else
            {
                value = For != null ? For.Model.ToString() : FieldValue;
            }
            
            var defaultColor = DefaultColor.EmptyNull();
            var isDefault = value.EqualsNoCase(defaultColor);
            var htmlAttributes = new Dictionary<string, object>
            {
                { "class", "form-control colorval" },
                { "placeholder", defaultColor }
            };

            if (FieldId.HasValue())
            {
                htmlAttributes.Add("id", FieldId);
            }

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.AppendCssClass("input-group colorpicker-component sm-colorbox");
            output.Attributes.Add("data-fallback-color", defaultColor);
            output.Content.AppendHtml(HtmlHelper.TextBox(For != null ? For.Name : FieldName, isDefault ? string.Empty : value, htmlAttributes));

            var addon = new TagBuilder("div");
            addon.AddCssClass("input-group-append input-group-addon");
            addon.InnerHtml.AppendHtml(
                $"<div class='input-group-text'><i class='thecolor' style='{(DefaultColor.HasValue() ? "background-color: " + DefaultColor : string.Empty)}'>&nbsp;</i></div>");

            output.Content.AppendHtml(addon);
        }
    }
}