using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("input", Attributes = "[type=password]")]
    public class InputPasswordTagHelper : TagHelper
    {
        const string EnableVisibilityToggleAttributeName = "sm-enable-visibility-toggle";

        // Must comer AFTER AspNetCore original TagHelper (Input | Select | TextArea)
        public override int Order => 100;

        /// <summary>
        /// Show visibility toggle button. Default = true.
        /// </summary>
        [HtmlAttributeName(EnableVisibilityToggleAttributeName)]
        public bool EnableVisibilityToggle { get; set; } = true;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (EnableVisibilityToggle)
            {
                output.PreElement.AppendHtml("<div class=\"toggle-pwd-group\">");
                output.PostElement.AppendHtml("<button type=\"button\" class=\"btn-toggle-pwd\"><i class=\"far fa-fw fa-eye-slash\"></i></button></div>");
            }
        }
    }
}
