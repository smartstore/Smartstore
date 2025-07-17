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
            // INFO: "aria-live=assertive" is redundant if "role=alert" is already set.
            // An element with "role=alert" is also always an assertive live region.
            // Using both can carry the risk of screen readers triggering double events.
            output.Attributes.SetAttributeNoReplace("role", "alert");
        }
    }
}