using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            var title = File.File.GetLocalized(x => x.Title)?.Value.NullEmpty();

            output.Attributes.SetAttribute("src", Src);
            output.AppendCssClass("file-preview");
            output.Attributes.SetAttributeNoReplace("title", title);
            output.Attributes.SetAttributeNoReplace("aria-label", title);
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

            var fileLink = new TagBuilder("a");
            fileLink.Attributes["src"] = Src;
            fileLink.Attributes["download"] = null;
            fileLink.InnerHtml.SetContent("file");
            
            var fallback = new TagBuilder("div");
            fallback.InnerHtml.SetHtmlContent($"Your browser does not support media files of type \"{File.MimeType}\". Download {fileLink.ToHtmlString()}.");

            output.PostElement.AppendHtml(fallback);
        }
    }
}
