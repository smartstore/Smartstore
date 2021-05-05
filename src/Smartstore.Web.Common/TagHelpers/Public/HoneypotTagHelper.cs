using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Security;

namespace Smartstore.Web.TagHelpers.Public
{
    [OutputElementHint("div")]
    [HtmlTargetElement("honeypot", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class HoneypotTagHelper : SmartTagHelper
    {
        const string EnabledAttributeName = "sm-enabled";

        private readonly HoneypotProtector _protector;
        private readonly SecuritySettings _securitySettings;

        public HoneypotTagHelper(HoneypotProtector protector, SecuritySettings securitySettings)
        {
            _protector = protector;
            _securitySettings = securitySettings;
        }

        /// <summary>
        /// Whether the hidden honeypot field should be rendered. Defaults to <c>true</c>.
        /// NOTE: The honeypot field is never rendered if honeypot is disabled by global settings.
        /// </summary>
        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        protected override string GenerateTagId(TagHelperContext context)
            => null;

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (!Enabled || !_securitySettings.EnableHoneypotProtection)
            {
                output.SuppressOutput();
                return;
            }

            var token = _protector.CreateToken();
            var serializedToken = _protector.SerializeToken(token);

            output.TagName = "div";
            output.AppendCssClass("d-none");
            output.TagMode = TagMode.StartTagAndEndTag;

            // Text field
            output.Content.AppendHtml(HtmlHelper.TextBox(token.Name, string.Empty, new { @class = "required-text-input", autocomplete = "off" }));

            // Hidden field
            output.Content.AppendHtml(HtmlHelper.Hidden(HoneypotProtector.TokenFieldName, serializedToken));
        }
    }
}
