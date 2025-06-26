using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("span", Attributes = "asp-validation-for")]
    public class ValidationTagHelper : TagHelper
    {
        public override int Order => int.MaxValue;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Screen readers recognize a text change and read it out immediately.
            if (!output.Attributes.ContainsName("role"))
            {
                output.Attributes.Add("role", "alert");
            }

            // A live region informs assistive technologies (AT) that the content may change without changing the focus, so it must be read.
            if (!output.Attributes.ContainsName("aria-live"))
            {
                output.Attributes.Add("aria-live", "assertive");
            }

            // It ensures that the entire message will be read again when an update is made (not just updated parts). 
            if (!output.Attributes.ContainsName("aria-atomic"))
            {
                output.Attributes.Add("aria-atomic", "true");
            }
        }
    }
}