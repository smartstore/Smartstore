using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Security;
using Smartstore.Utilities;

namespace Smartstore.Web.TagHelpers.Public
{
    [OutputElementHint("div")]
    [HtmlTargetElement("captcha", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class CaptchaTagHelper : TagHelper
    {
        const string EnabledAttributeName = "sm-enabled";

        private readonly CaptchaSettings _captchaSettings;
        private readonly IWorkContext _workContext;
        private readonly SmartConfiguration _appConfig;

        public CaptchaTagHelper(CaptchaSettings captchaSettings, IWorkContext workContext, SmartConfiguration appConfig)
        {
            _captchaSettings = captchaSettings;
            _workContext = workContext;
            _appConfig = appConfig;
        }

        /// <summary>
        /// Whether the captcha box should be rendered. Defaults to <c>true</c>.
        /// NOTE: The captcha box is never rendered if captchas are disabled by global settings
        /// or if given settings are invalid.
        /// </summary>
        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!Enabled || !_captchaSettings.CanDisplayCaptcha)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "div";
            output.AppendCssClass("captcha-box");

            var widgetUrl = _appConfig.Google.RecaptchaWidgetUrl;
            var ident = CommonHelper.GenerateRandomDigitCode(5);
            var elementId = "recaptcha" + ident;
            var siteKey = _captchaSettings.ReCaptchaPublicKey;
            var callbackName = "recaptchaOnload" + ident;

            var url = "{0}?onload={1}&render=explicit&hl={2}".FormatInvariant(
                widgetUrl,
                callbackName,
                _workContext.WorkingLanguage.UniqueSeoCode.EmptyNull().ToLower()
            );

            var script = new[]
            {
                "<script>",
                "	var {0} = function() {{".FormatInvariant(callbackName),
                "		renderGoogleRecaptcha('{0}', '{1}', {2});".FormatInvariant(elementId, siteKey, _captchaSettings.UseInvisibleReCaptcha.ToString().ToLower()),
                "	};",
                "</script>",
                "<div id='{0}' class='g-recaptcha' data-sitekey='{1}'></div>".FormatInvariant(elementId, siteKey),
                "<script src='{0}' async defer></script>".FormatInvariant(url),
            }.StrJoin("");

            output.TagMode = TagMode.StartTagAndEndTag;
            output.Content.SetHtmlContent(script);
        }
    }
}
