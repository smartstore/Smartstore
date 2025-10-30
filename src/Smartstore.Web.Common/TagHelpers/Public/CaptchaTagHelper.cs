using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;

namespace Smartstore.Web.TagHelpers.Public
{
    /// <summary>
    /// TagHelper that renders a provider-agnostic CAPTCHA container.
    /// - Wraps the widget HTML emitted by the active ICaptchaProvider.
    /// - Adds normalized attributes and ARIA for accessibility.
    /// - Does not contain any provider-specific logic.
    /// </summary>
    [OutputElementHint("div")]
    [HtmlTargetElement("captcha", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class CaptchaTagHelper : TagHelper
    {
        const string EnabledAttributeName = "sm-enabled";
        const string TargetAttributeName = "sm-target";

        private readonly IWorkContext _workContext;
        private readonly ICaptchaManager _captchaManager;
        private readonly Localizer T;

        public CaptchaTagHelper(
            IWorkContext workContext,
            ICaptchaManager captchaManager,
            Localizer localizer)
        {
            _workContext = workContext;
            _captchaManager = captchaManager;
            T = localizer;
        }

        /// <summary>
        /// Controls whether the CAPTCHA box should be rendered. This option precedes <see cref="Target"/>.
        /// Note: If captcha is globally disabled or misconfigured, nothing is rendered regardless of this flag.
        /// </summary>
        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the CAPTCHA target (see <see cref="CaptchaSettings.Targets"/>).
        /// If target is not active, nothing is rendered, regardless of <see cref="Enabled"/>.
        /// </summary>
        [HtmlAttributeName(TargetAttributeName)]
        public string Target { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Do not render when disabled, target is inactive or when no provider is configured.
            if (!Enabled || !_captchaManager.IsActiveTarget(Target) || !_captchaManager.IsConfigured(out var provider))
            {
                output.SuppressOutput();
                return;
            }

            var captchaProvider = provider.Value;
            var captchaContext = new CaptchaContext(ViewContext.HttpContext, _workContext.WorkingLanguage);
            var widget = await captchaProvider.CreateWidgetAsync(captchaContext);
            if (widget == null)
            {
                output.SuppressOutput();
                return;
            }

            // Ask the provider to create its widget (HTML fragment).
            var html = await widget.InvokeAsync(ViewContext);
            if (!html.HasContent())
            {
                output.SuppressOutput();
                return;
            }

            // Prepare the outer container
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;

            // Base class for styling and to make it discoverable by the client bootstrap
            output.AppendCssClass("captcha-box");

            // Accessibility: mark as region and provide a localized label
            output.Attributes.SetAttribute("role", "region");
            output.Attributes.SetAttribute("aria-label", T("Common.SecurityPrompt").Value);

            // Provider metadata (agnostic): expose system name and mode for the generic client bootstrap
            var systemName = provider.Metadata.SystemName;
            output.Attributes.SetAttribute("data-captcha-provider", systemName);

            // Derive a normalized mode purely from the standard contract
            // - "interactive"  : visible widget (e.g., v2 checkbox)
            // - "invisible"    : v2 invisible
            // - "score"        : v3 (non-interactive, score-based)
            var mode = captchaProvider.IsNonInteractive
                ? (captchaProvider.IsInvisible ? "invisible" : "score")
                : "interactive";

            output.Attributes.SetAttribute("data-captcha-mode", mode);

            // Finally emit the provider HTML (placeholder div for v2, hidden input for v3, etc.)
            output.Content.SetHtmlContent(html);

            // Register required CAPTCHA bootstrapper
            captchaContext.AssetBuilder.AppendHeadScriptFiles("/js/smartstore.captcha.js");

            // INFO:
            // Any provider-specific client config (e.g., element id, site key) should be emitted
            // by the provider inside the HTML fragment (e.g., <script type="application/json" class="captcha-config">...).
            // The generic captcha-bootstrap.js will read it and initialize the correct adapter.
        }
    }
}
