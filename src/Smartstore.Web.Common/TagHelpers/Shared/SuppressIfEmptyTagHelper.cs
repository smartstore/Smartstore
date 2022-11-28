using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Suppresses tag output if child content is empty (due to some conditional logic).
    /// </summary>
    [HtmlTargetElement("div", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("span", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("section", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("nav", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("aside", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("p", Attributes = SuppressAttributeName)]
    public class SuppressIfEmptyTagHelper : TagHelper
    {
        const string SuppressAttributeName = "sm-suppress-if-empty";

        // Should run after IfTagHelper
        public override int Order => int.MinValue + 2000;

        /// <summary>
        /// Suppresses tag output if child content is empty (due to some conditional logic).
        /// </summary>
        [HtmlAttributeName(SuppressAttributeName)]
        public bool SuppressIfEmpty { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!SuppressIfEmpty)
            {
                return;
            }
            
            var childContent = await output.GetChildContentAsync();
            if (childContent.IsEmptyOrWhiteSpace)
            {
                output.SuppressOutput();
            }
        }
    }
}
