using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("file-icon", Attributes = FileExtensionAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class FileIconTagHelper : TagHelper
    {
        const string FileExtensionAttributeName = "file-extension";
        const string LabelAttributeName = "label";
        const string ShowLabelAttributeName = "show-label";
        const string BadgeClassAttributeName = "badge-class";

        /// <summary>
        /// Specifies the file extension.
        /// </summary>
        [HtmlAttributeName(FileExtensionAttributeName)]
        public string FileExtension { get; set; }

        /// <summary>
        /// Specifies the label text. <see cref="FileExtension"/> by default.
        /// </summary>
        [HtmlAttributeName(LabelAttributeName)]
        public string Label { get; set; }

        /// <summary>
        /// A value indicating whether to show the file extension also as a label.
        /// </summary>
        [HtmlAttributeName(ShowLabelAttributeName)]
        public bool ShowLabel { get; set; }

        /// <summary>
        /// Specifies a badge class name, e.g. "badge-info".
        /// </summary>
        [HtmlAttributeName(BadgeClassAttributeName)]
        public string BadgeClass { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.TagMode = TagMode.StartTagAndEndTag;

            if (ShowLabel && FileExtension.IsEmpty())
            {
                // No icon, just "n\a" label.
                output.Content.AppendHtml($"<span class='text-muted'>{string.Empty.NaIfEmpty()}</span>");
            }
            else
            {
                var ext = FileExtension.EmptyNull().TrimStart('.');

                var iconClass = ext.ToLowerInvariant() switch
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

                ext = ext.NaIfEmpty().ToUpper();

                output.Content.AppendHtml($"<i class='fa-fw {iconClass}' title='{ext}'></i>");

                if (ShowLabel)
                {
                    output.Content.AppendHtml("<span class='ml-1{0}'>{1}</span>".FormatInvariant(
                        FileExtension.IsEmpty() ? " text-muted" : "",
                        Label ?? ext));
                }
            }

            if (BadgeClass.HasValue())
            {
                output.WrapHtmlInside($"<span class='badge{BadgeClass.LeftPad()}'>", "</span>");
            }
        }
    }
}
