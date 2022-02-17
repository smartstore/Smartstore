using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("svg")]
    [HtmlTargetElement("bootstrap-icon", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class BootstrapIconTagHelper : TagHelper
    {
        const string NameAttributeName = "name";

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

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

            var urlHelper = ViewContext.HttpContext.RequestServices.GetService<IUrlHelper>();
            var url = urlHelper.Content($"~/lib/bi/bootstrap-icons.svg#{Name}");
            output.Content.AppendHtml($"<use xlink:href=\"{url}\" />");
        }
    }
}
