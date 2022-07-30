using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Outputs a rich <c>figure</c> tag that contains a thumbnail view of a media file.
    /// An applicable icon will be displayed when thumbnail creation is not possible.
    /// </summary>
    [OutputElementHint("figure")]
    [HtmlTargetElement(ThumbnailTagName, Attributes = FileAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement(ThumbnailTagName, Attributes = FileIdAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement(ThumbnailTagName, Attributes = ModelAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class ThumbnailTagHelper : BaseImageTagHelper
    {
        const string ThumbnailTagName = "media-thumbnail";

        protected override void ProcessMedia(TagHelperContext context, TagHelperOutput output)
        {
            if (Src.IsEmpty())
            {
                output.SuppressOutput();
                return;
            }

            var mediaType = File?.MediaType ?? MediaType.Image;

            // Root
            output.TagName = "figure";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.AppendCssClass("file-figure");

            // Build <i/>
            var iconHint = GetIconHint(mediaType);
            var ic = new TagBuilder("i");
            ic.Attributes["class"] = "file-icon show fa-5x fa-fw " + iconHint.Name;
            ic.Attributes["style"] = "color: " + iconHint.Color;

            // Append <i/> to root <figure/>
            output.Content.AppendHtml(ic);

            // Build <picture/>
            var picture = new TagBuilder("picture");
            picture.Attributes["class"] = "file-thumb";
            picture.Attributes["data-type"] = mediaType;
            output.Attributes.MoveAttribute(picture, "title");
            picture.MergeAttribute("title", () => Model?.Title ?? File?.File?.GetLocalized(x => x.Title).Value.NullEmpty(), false, true);

            // Build <img/>
            var img = new TagBuilder("img") { TagRenderMode = TagRenderMode.SelfClosing };
            img.Attributes["class"] = "file-img";
            img.Attributes["data-src"] = Src;
            img.MergeAttribute("alt", () => Model?.Alt ?? File?.File?.GetLocalized(x => x.Alt).Value.NullEmpty(), false, true);

            // picture > img
            picture.InnerHtml.SetHtmlContent(img);

            // Append <picture/> to root <figure/>
            output.Content.AppendHtml(picture);
        }

        private static (string Name, string Color) GetIconHint(string mediaType)
        {
            return mediaType switch
            {
                "image" => ("far fa-file-image", "#e77c00"),
                "video" => ("far fa-file-video", "#ff5722"),
                "audio" => ("far fa-file-audio", "#009688"),
                "document" => ("fas fa-file-alt", "#2b579a"),
                "text" => ("far fa-file-alt", "#607d8B"),
                _ => ("far fa-file", "#bbb"),
            };
        }
    }
}