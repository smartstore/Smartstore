using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Adds applicable CSS classes to validation elements.
    /// </summary>
    [HtmlTargetElement("span", Attributes = ValidationForAttribute)]
    [HtmlTargetElement("div", Attributes = ValidationSummaryAttribute)]
    public class ValidationTagHelper : TagHelper
    {
        const string ValidationForAttribute = "asp-validation-for";
        const string ValidationSummaryAttribute = "asp-validation-summary";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // TODO: (core) Remove CSS formatting rules from stylesheets.
            
            if (context.AllAttributes.ContainsName(ValidationForAttribute))
            {
                output.AppendCssClass("text-danger");
            }
            else if (context.AllAttributes.ContainsName(ValidationSummaryAttribute))
            {
                output.AppendCssClass("alert alert-danger");
            }
        }
    }
}
