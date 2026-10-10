using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared;

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
        if (context.ShouldSuppressChildContent())
        {
            return;
        }

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

            var iconName = ext.ToLowerInvariant() switch
            {
                "pdf" => "file-pdf",
                "doc" or "docx" or "docm" or "odt" or "dot" or "dotx" or "dotm" => "file-word",
                "xls" or "xlsx" or "xlsm" or "xlsb" or "ods" => "file-spreadsheet",
                "csv" or "tab" => "file-csv",
                "ppt" or "pptx" or "pptm" or "ppsx" or "odp" or "potx" or "pot" or "potm" or "pps" or "ppsm" => "file-presentation",
                "zip" or "rar" or "7z" => "file-archive",
                "png" or "jpg" or "jpeg" or "bmp" or "psd" => "file-image",
                "mp3" or "wav" or "ogg" or "wma" => "file-audio",
                "mp4" or "mkv" or "wmv" or "avi" or "asf" or "mpg" or "mpeg" => "file-video",
                "txt" => "file-text",
                "exe" => "gear",
                "xml" or "html" or "htm" => "file-code",
                _ => "file",
            };

            ext = ext.NaIfEmpty().ToUpper();

            output.Content.AppendHtml($"<sm-icon name='{iconName}' fw title='{ext}'></sm-icon>");

            if (ShowLabel)
            {
                output.Content.AppendHtml("<span class='ml-1{0}'>{1}</span>".FormatInvariant(
                    FileExtension.IsEmpty() ? " text-muted" : "",
                    Label ?? ext));
            }
        }

        if (BadgeClass.HasValue())
        {
            output.PreElement.AppendHtml($"<span class='badge{BadgeClass.LeftPad()}'>");
            output.PostElement.PrependHtml("</span>");
        }
    }
}