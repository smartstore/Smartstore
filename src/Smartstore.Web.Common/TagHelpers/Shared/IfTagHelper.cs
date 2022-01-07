using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("*", Attributes = IfAttributeName)]
    public class IfTagHelper : TagHelper
    {
        const string IfAttributeName = "sm-if";

        public override int Order => int.MinValue;

        /// <summary>
        /// A condition to check before outputting the tag.
        /// <c>false</c> will suppress the output completely.
        /// </summary>
        [HtmlAttributeName(IfAttributeName)]
        public bool Condition { get; set; } = true;

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!Condition)
            {
                output.SuppressOutput();
            }

            return Task.CompletedTask;
        }
    }
}
