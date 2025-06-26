using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Adds assistive aria attributes to a <span> element that is used to display validation messages.
    /// </summary>
    [HtmlTargetElement("span", Attributes = "asp-validation-for")]
    public class ValidationTagHelper : TagHelper
    {
        public override int Order => int.MaxValue;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Screen readers recognize a text change and read it out immediately.
            output.Attributes.SetAttributeNoReplace("role", "alert");

            // A live region informs assistive technologies (AT) that the content may change without changing the focus, so it must be read.
            output.Attributes.SetAttributeNoReplace("aria-live", "assertive");

            // It ensures that the entire message will be read again when an update is made (not just updated parts). 
            output.Attributes.SetAttributeNoReplace("aria-atomic", "true");
        }
    }
}