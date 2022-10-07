using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("a", Attributes = BackToAttributeName)]
    public class BackToTagHelper : TagHelper
    {
        const string BackToAttributeName = "sm-backto";

        public BackToTagHelper(Localizer localizer)
        {
            T = localizer;
        }

        private Localizer T { get; }

        /// <summary>
        /// Renders the link as a regular button with a left arrow icon.
        /// </summary>
        [HtmlAttributeName(BackToAttributeName)]
        public bool IsBackButton { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!IsBackButton)
            {
                return;
            }
            
            output.AppendCssClass("btn btn-outline-secondary btn-sm btn-icon mr-3");

            if (!output.Attributes.ContainsName("title"))
            {
                output.Attributes.SetAttribute("title", T("Admin.Common.BackToList"));
            }

            output.Content.Clear();
            output.Content.SetHtmlContent("<i class=\"fa fa-arrow-left\"></i>");
        }
    }
}
