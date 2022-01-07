using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement(VideoTagName, Attributes = FileAttributeName)]
    [HtmlTargetElement(VideoTagName, Attributes = FileIdAttributeName)]
    public class VideoTagHelper : BaseMediaTagHelper
    {
        const string VideoTagName = "video";

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

    [HtmlTargetElement(SourceTagName, Attributes = FileAttributeName)]
    [HtmlTargetElement(SourceTagName, Attributes = FileIdAttributeName)]
    public class SourceTagHelper : BaseMediaTagHelper
    {
        const string SourceTagName = "source";

        protected override void ProcessMedia(TagHelperContext context, TagHelperOutput output)
        {
            if (Src.IsEmpty() || File == null)
            {
                output.SuppressOutput();
                return;
            }

            output.MergeAttribute("src", Src);
            output.MergeAttribute("type", File.MimeType);
        }
    }
}
