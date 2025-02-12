using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Smartstore.Core.Localization;
using Smartstore.Net;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement(ImageTagName, Attributes = FileAttributeName)]
    [HtmlTargetElement(ImageTagName, Attributes = FileIdAttributeName)]
    [HtmlTargetElement(ImageTagName, Attributes = ModelAttributeName)]
    public class ImageTagHelper : BaseImageTagHelper
    {
        const string ImageTagName = "img";
        const string AppendVersionAttributeName = "asp-append-version";

        /// <summary>
        /// Gets or sets a value indicating whether to append a version query string parameter to the image URL. Default = false.
        /// </summary>
        [HtmlAttributeName(AppendVersionAttributeName)]
        public bool AppendVersion { get; set; }

        protected override void ProcessMedia(TagHelperContext context, TagHelperOutput output)
        {
            if (Src.IsEmpty())
            {
                output.SuppressOutput();
                return;
            }

            var src = Src;
            if (AppendVersion && File != null && src.HasValue())
            {
                src = QueryHelpers.AddQueryString(src, "v", ETagUtility.GenerateETag(File.LastModified, File.Length, null, true));
            }

            output.AppendCssClass("file-img");
            output.Attributes.SetAttribute("src", src);

            output.Attributes.SetAttributeNoReplace("alt", () => Model?.Alt ?? File?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty());
            output.Attributes.SetAttributeNoReplace("title", () => Model?.Title ?? File?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty());
        }
    }
}