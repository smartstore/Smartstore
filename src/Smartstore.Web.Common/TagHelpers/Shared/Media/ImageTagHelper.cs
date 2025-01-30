using Microsoft.AspNetCore.Razor.TagHelpers;
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
        const string AppendVersionAttributeName = "sm-append-version";

        // TODO: (mg) Don't reinvent the wheel. Inbuilt ImageTagHelper has already asp-append-version, that uses IFileVersionProvider internally.
        // RE: Yes it has but we cannot use it. It only works with static files. See Microsoft.AspNetCore.Mvc.Razor.Infrastructure.DefaultFileVersionProvider
        // line 89: It opens a stream to the file and calculates a hash from it. The check "fileInfo.Exists" in line 72 is never "true" in our case,
        // thus the version query string is never appended. Maybe we should implement our own IFileVersionProvider which generates a version based on MediaFile infos?
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
            if (AppendVersion && Model.File != null && src.HasValue())
            {
                src = src + (src.Contains('?') ? '&' : '?') + "ver=" + ETagUtility.GenerateETag(Model.File.UpdatedOnUtc, Model.File.Size, null, true);
            }

            output.AppendCssClass("file-img");
            output.Attributes.SetAttribute("src", src);

            output.Attributes.SetAttributeNoReplace("alt", () => Model?.Alt ?? File?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty());
            output.Attributes.SetAttributeNoReplace("title", () => Model?.Title ?? File?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty());
        }
    }
}