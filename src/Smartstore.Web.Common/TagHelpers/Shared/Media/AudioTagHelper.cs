using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement(AudioTagName, Attributes = FileAttributeName)]
    [HtmlTargetElement(AudioTagName, Attributes = FileIdAttributeName)]
    public class AudioTagHelper : BaseMediaTagHelper
    {
        const string AudioTagName = "audio";

        protected override void ProcessMedia(TagHelperContext context, TagHelperOutput output)
        {
            if (Src.IsEmpty() || File == null)
            {
                output.SuppressOutput();
                return;
            }

            output.Attributes.SetAttribute("src", Src);
            output.AppendCssClass("file-preview");
            output.Attributes.SetAttributeNoReplace("title", () => File.File.GetLocalized(x => x.Title)?.Value.NullEmpty());
            output.Attributes.SetAttributeNoReplace("preload", "metadata");

            if (!output.Attributes.ContainsName("controls"))
            {
                output.Attributes.Add(new TagHelperAttribute("controls", null, HtmlAttributeValueStyle.Minimized));
            }
        }
    }
}
