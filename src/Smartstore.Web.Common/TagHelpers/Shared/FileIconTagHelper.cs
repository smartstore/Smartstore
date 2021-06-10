using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("file-icon", Attributes = FileExtensionAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class FileIconTagHelper : TagHelper
    {
        const string FileExtensionAttributeName = "file-extension";
        const string RenderLabelAttributeName = "render-label";

        /// <summary>
        /// Specifies the file extension.
        /// </summary>
        [HtmlAttributeName(FileExtensionAttributeName)]
        public string FileExtension { get; set; }

        /// <summary>
        /// A value indicating whether to render the file extension also as a label.
        /// </summary>
        [HtmlAttributeName(RenderLabelAttributeName)]
        public bool RenderLabel { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string icon = null;
            var ext = FileExtension.EmptyNull();

            if (ext.StartsWith('.'))
            {
                ext = ext[1..];
            }

            icon = ext.ToLowerInvariant() switch
            {
                "pdf" => "far fa-file-pdf",
                "doc" or "docx" or "docm" or "odt" or "dot" or "dotx" or "dotm" => "far fa-file-word",
                "xls" or "xlsx" or "xlsm" or "xlsb" or "ods" => "far fa-file-excel",
                "csv" or "tab" => "fa fa-file-csv",
                "ppt" or "pptx" or "pptm" or "ppsx" or "odp" or "potx" or "pot" or "potm" or "pps" or "ppsm" => "far fa-file-powerpoint",
                "zip" or "rar" or "7z" => "far fa-file-archive",
                "png" or "jpg" or "jpeg" or "bmp" or "psd" => "far fa-file-image",
                "mp3" or "wav" or "ogg" or "wma" => "far fa-file-audio",
                "mp4" or "mkv" or "wmv" or "avi" or "asf" or "mpg" or "mpeg" => "far fa-file-video",
                "txt" => "far fa-file-alt",
                "exe" => "fa fa-cog",
                "xml" or "html" or "htm" => "far fa-file-code",
                _ => "far fa-file",
            };

            var label = ext.NaIfEmpty().ToUpper();

            if (RenderLabel && ext.IsEmpty())
            {
                // No icon, just "n\a" label.
                output.TagName = "span";
                output.TagMode = TagMode.StartTagAndEndTag;
                output.AppendCssClass("text-muted");
                output.Content.Append(string.Empty.NaIfEmpty());
            }
            else
            {
                output.TagName = "i";
                output.TagMode = TagMode.StartTagAndEndTag;
                output.AppendCssClass("fa-fw " + icon);
                output.Attributes.Add("title", label);

                if (RenderLabel)
                {
                    var labelSpan = new TagBuilder("span");
                    labelSpan.AppendCssClass(ext.IsEmpty() ? "text-muted" : "ml-1");
                    labelSpan.InnerHtml.Append(label.NaIfEmpty());

                    output.PostElement.AppendHtml(labelSpan);
                }
            }
        }
    }
}
