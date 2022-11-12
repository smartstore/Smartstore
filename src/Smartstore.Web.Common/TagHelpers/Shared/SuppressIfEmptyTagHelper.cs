using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Suppresses tag output if child content is empty (due to some conditional logic).
    /// </summary>
    [HtmlTargetElement("div", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("span", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("section", Attributes = SuppressAttributeName)]
    public class SuppressIfEmptyTagHelper : TagHelper
    {
        const string SuppressAttributeName = "sm-suppress-if-empty";

        /// <summary>
        /// Suppresses tag output if child content is empty (due to some conditional logic).
        /// </summary>
        [HtmlAttributeName(SuppressAttributeName)]
        public bool SuppressIfEmpty { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var childContent = await output.GetChildContentAsync();
            if (childContent.IsEmptyOrWhiteSpace)
            {
                output.SuppressOutput();
            }
        }
    }
}
