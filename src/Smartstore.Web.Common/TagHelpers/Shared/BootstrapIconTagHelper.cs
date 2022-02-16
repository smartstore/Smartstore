using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("svg")]
    [HtmlTargetElement("bootstrap-icon", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class BootstrapIconTagHelper : TagHelper
    {
        const string NameAttributeName = "name";

        /// <summary>
        /// The name of the Bootstrap icon to use.
        /// </summary>
        [HtmlAttributeName(NameAttributeName)]
        public string Name { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "svg";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.AppendCssClass("bi");
            output.MergeAttribute("fill", "currentColor");

            output.Content.AppendHtml($"<use xlink:href=\"/lib/bi/bootstrap-icons.svg#{Name}\" />");
        }
    }
}
