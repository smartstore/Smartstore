using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("small")]
    [HtmlTargetElement("hint", Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class HintTagHelper : BaseFormTagHelper
    {
        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            var hintText = For.Metadata.Description;

            if (hintText.IsEmpty())
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "small";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.AppendCssClass("form-text text-muted");
            output.Content.SetContent(hintText);
        }
    }
}