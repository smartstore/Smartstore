using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("sm-colorbox", TagStructure = TagStructure.WithoutEndTag)]
    public class ColorBoxTagHelper : BaseFormTagHelper
    {
        [HtmlAttributeName("color")]
        public string Color { get; set; }

        [HtmlAttributeName("default-color")]
        public string DefaultColor { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            base.ProcessCore(context, output);

            if (!Color.HasValue() || !DefaultColor.HasValue())
            {
                return;
            }

            DefaultColor = DefaultColor.EmptyNull();
            var isDefault = Color.EqualsNoCase(DefaultColor);

            var cnt = new TagBuilder("div");
            cnt.AddCssClass("input-group colorpicker-component sm-colorbox colorpicker-element");
            cnt.Attributes.Add("data-fallback-color", DefaultColor);

            // Move CCS classes from output to cnt and clear output classes.
            var cssClasses = output.Attributes["class"]?.Value?.ToString();
            cnt.AddCssClass(cssClasses);
            output.MergeAttribute("class", "", true);

            var addon = new TagBuilder("div");
            addon.AddCssClass("input-group-append input-group-addon");
            addon.InnerHtml.AppendHtml("<div class=\"input-group-text\"><i class=\"thecolor\" style=\"{0}\">&nbsp;</i></div>".FormatWith(DefaultColor.HasValue() ? "background-color: " + DefaultColor : ""));

            output.WrapElementWith(cnt);
            output.PostContent.AppendHtml(addon);

            // Generate main <input/>
            output.TagName = "input";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.MergeAttribute("id", HtmlHelper.GenerateIdFromName(For.Name), true);
            output.MergeAttribute("name", For.Name, true);                                  // TODO: (mh) (core) Test model binding.
            output.AppendCssClass("form-control colorval");
            output.Attributes.Add("placeholder", Color ?? DefaultColor);
            output.Attributes.Add("type", "text");
            output.Attributes.Add("value", isDefault ? "" : Color);
        }
    }
}