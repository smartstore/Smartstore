using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement(ImageTagName, Attributes = FileAttributeName)]
    [HtmlTargetElement(ImageTagName, Attributes = FileIdAttributeName)]
    [HtmlTargetElement(ImageTagName, Attributes = ModelAttributeName)]
    public class ImageTagHelper : BaseImageTagHelper
    {
        const string ImageTagName = "img";

        protected override void ProcessMedia(TagHelperContext context, TagHelperOutput output)
        {
            if (Src.IsEmpty())
            {
                output.SuppressOutput();
                return;
            }

            output.AppendCssClass("file-img");
            output.Attributes.SetAttribute("src", Src);

            output.Attributes.SetAttributeNoReplace("alt", () => Model?.Alt ?? File?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty());
            output.Attributes.SetAttributeNoReplace("title", () => Model?.Title ?? File?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty());
        }
    }
}