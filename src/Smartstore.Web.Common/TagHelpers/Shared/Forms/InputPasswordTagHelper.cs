using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Data;

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

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (EnableVisibilityToggle)
            {
                output.PreElement.AppendHtml("<div class=\"toggle-pwd-group\">");

                string htmlAttributes = null;
                if (DataSettings.DatabaseIsInstalled())
                {
                    var T = ViewContext.HttpContext.GetServiceScope().Resolve<Localizer>();
                    var id = output.Attributes.TryGetAttribute("id", out var idAttr) ? idAttr.ValueAsString().NullEmpty() : null;

                    htmlAttributes = $"aria-pressed=\"false\" aria-controls=\"{id ?? string.Empty}\" aria-label=\"{T("Aria.Label.ShowPassword")}\"";
                }
                else
                {
                    htmlAttributes = "aria-hidden=\"true\"";
                }

                output.PostElement.AppendHtml($"<button type=\"button\" class=\"btn-toggle-pwd\" {htmlAttributes}><i class=\"far fa-fw fa-eye-slash\" aria-hidden=\"true\"></i></button></div>");
            }
        }
    }
}
