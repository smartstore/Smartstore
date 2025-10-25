using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.TagHelpers.Public
{
    [OutputElementHint("div")]
    [HtmlTargetElement("captcha", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class CaptchaTagHelper : TagHelper
    {
        const string EnabledAttributeName = "sm-enabled";

        private readonly CaptchaSettings _captchaSettings;
        private readonly IWorkContext _workContext;
        private readonly IProviderManager _providerManager;
        private readonly Localizer T;

        public CaptchaTagHelper(
            CaptchaSettings captchaSettings, 
            IWorkContext workContext,
            IProviderManager providerManager,
            Localizer localizer)
        {
            _captchaSettings = captchaSettings;
            _workContext = workContext;
            _providerManager = providerManager;
            T = localizer;
        }

        /// <summary>
        /// Whether the captcha box should be rendered. Defaults to <c>true</c>.
        /// NOTE: The captcha box is never rendered if captchas are disabled by global settings
        /// or if given settings are invalid.
        /// </summary>
        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!Enabled || !_captchaSettings.Enabled || _captchaSettings.ShowOn.Length == 0)
            {
                output.SuppressOutput();
                return;
            }

            var captchaProvider = _providerManager.GetProvider<ICaptchaProvider>(_captchaSettings.ProviderSystemName)?.Value;
            if (captchaProvider == null || !captchaProvider.IsConfigured)
            {
                output.SuppressOutput();
                return;
            }

            var captchaContext = new CaptchaContext(ViewContext.HttpContext, _workContext.WorkingLanguage);
            var widget = await captchaProvider.CreateWidgetAsync(captchaContext);
            if (widget == null)
            {
                output.SuppressOutput();
                return;
            }

            var html = await widget.InvokeAsync(ViewContext);
            if (!html.HasContent())
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "div";
            output.AppendCssClass("captcha-box");
            output.Attributes.SetAttribute("role", "region");
            output.Attributes.SetAttribute("aria-label", T("Common.SecurityPrompt").Value);

            output.TagMode = TagMode.StartTagAndEndTag;
            output.Content.SetHtmlContent(html);
        }
    }
}
