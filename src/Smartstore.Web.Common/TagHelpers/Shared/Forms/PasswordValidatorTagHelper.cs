using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Renders a password policy widget and exposes the effective password policy via <c>data-*</c> attributes
    /// so that client-side code can provide interactive feedback while typing.
    /// </summary>
    [HtmlTargetElement("password-validator", Attributes = ForAttributeName)]
    public class PasswordValidatorTagHelper(CustomerSettings customerSettings) : SmartTagHelper
    {
        private sealed class PasswordPolicy
        {
            public int MinLength { get; init; }
            public bool RequireLower { get; init; }
            public bool RequireUpper { get; init; }
            public bool RequireDigit { get; init; }
            public bool RequireNonAlpha { get; init; }
            public int UniqueChars { get; init; }
        }

        private const string ForAttributeName = "asp-for";

        private CustomerSettings CustomerSettings { get; } = customerSettings;

        /// <summary>
        /// The model expression to resolve the associated password field.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        /// <summary>
        /// Overrides the minimum password length.
        /// </summary>
        [HtmlAttributeName("min-length")]
        public int? MinLength { get; set; }

        /// <summary>
        /// Overrides whether the password requires at least one lowercase character.
        /// </summary>
        [HtmlAttributeName("require-lower")]
        public bool? RequireLower { get; set; }

        /// <summary>
        /// Overrides whether the password requires at least one uppercase character.
        /// </summary>
        [HtmlAttributeName("require-upper")]
        public bool? RequireUpper { get; set; }

        /// <summary>
        /// Overrides whether the password requires at least one digit.
        /// </summary>
        [HtmlAttributeName("require-digit")]
        public bool? RequireDigit { get; set; }

        /// <summary>
        /// Overrides whether the password requires at least one non-alphanumeric character.
        /// </summary>
        [HtmlAttributeName("require-nonalpha")]
        public bool? RequireNonAlpha { get; set; }

        /// <summary>
        /// Overrides how many unique characters are required.
        /// </summary>
        [HtmlAttributeName("uniquechars")]
        public int? UniqueChars { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;

            var id = TagBuilder.CreateSanitizedId(For.Name, "_");
            var policy = GetPasswordPolicy();

            var content = new DefaultTagHelperContent();

            var wrap = new TagBuilder("div");
            wrap.AppendCssClass("pwd-policy-wrap collapse");
            wrap.Attributes["data-pwd-policy-for"] = id;
            wrap.Attributes["aria-hidden"] = "true";

            var policyBox = new TagBuilder("div");
            policyBox.AppendCssClass("pwd-policy small");

            var padding = new TagBuilder("div");
            padding.AddCssClass("p-2");

            var title = new TagBuilder("div");
            title.AppendCssClass("fwm mb-2");
            title.InnerHtml.AppendHtml((IHtmlContent)T("Account.Register.Result.MeetPasswordRules", string.Empty));

            var ul = new TagBuilder("ul");
            ul.AppendCssClass("fa-ul pwd-rules mb-0");

            if (policy.MinLength > 0)
            {
                ul.InnerHtml.AppendHtml(RenderRule("minlength", T("Identity.Error.PasswordTooShort", policy.MinLength)));
            }
            if (policy.UniqueChars > 0)
            {
                ul.InnerHtml.AppendHtml(RenderRule("uniquechars", T("Identity.Error.PasswordRequiresUniqueChars", policy.UniqueChars)));
            }
            if (policy.RequireLower)
            {
                ul.InnerHtml.AppendHtml(RenderRule("lower", T("Identity.Error.PasswordRequiresLower")));
            }
            if (policy.RequireUpper)
            {
                ul.InnerHtml.AppendHtml(RenderRule("upper", T("Identity.Error.PasswordRequiresUpper")));
            }
            if (policy.RequireDigit)
            {
                ul.InnerHtml.AppendHtml(RenderRule("digit", T("Identity.Error.PasswordRequiresDigit")));
            }
            if (policy.RequireNonAlpha)
            {
                ul.InnerHtml.AppendHtml(RenderRule("nonalpha", T("Identity.Error.PasswordRequiresNonAlphanumeric")));
            }

            padding.InnerHtml.AppendHtml(title);
            padding.InnerHtml.AppendHtml(ul);
            policyBox.InnerHtml.AppendHtml(padding);
            wrap.InnerHtml.AppendHtml(policyBox);

            var dataHost = new TagBuilder("span");
            dataHost.AddCssClass("pwd-policy-data");
            dataHost.Attributes["hidden"] = "hidden";
            dataHost.Attributes["data-pwd-policy-host"] = id;
            dataHost.Attributes["data-min-length"] = policy.MinLength.ToString();
            dataHost.Attributes["data-require-lower"] = policy.RequireLower.ToStringLower();
            dataHost.Attributes["data-require-upper"] = policy.RequireUpper.ToStringLower();
            dataHost.Attributes["data-require-digit"] = policy.RequireDigit.ToStringLower();
            dataHost.Attributes["data-require-nonalpha"] = policy.RequireNonAlpha.ToStringLower();
            dataHost.Attributes["data-uniquechars"] = policy.UniqueChars.ToString();
            dataHost.Attributes["data-msg-meet"] = T("Account.Register.Result.MeetPasswordRules", string.Empty);
            dataHost.Attributes["data-msg-minlength"] = T("Identity.Error.PasswordTooShort", policy.MinLength);
            dataHost.Attributes["data-msg-lower"] = T("Identity.Error.PasswordRequiresLower");
            dataHost.Attributes["data-msg-upper"] = T("Identity.Error.PasswordRequiresUpper");
            dataHost.Attributes["data-msg-digit"] = T("Identity.Error.PasswordRequiresDigit");
            dataHost.Attributes["data-msg-nonalpha"] = T("Identity.Error.PasswordRequiresNonAlphanumeric");
            dataHost.Attributes["data-msg-uniquechars"] = T("Identity.Error.PasswordRequiresUniqueChars", policy.UniqueChars);

            wrap.InnerHtml.AppendHtml(dataHost);

            content.AppendHtml(wrap);
            output.Content.SetHtmlContent(content);

            // Script init (module) for this field.
            var script = new TagBuilder("script");
            script.Attributes["type"] = "module";
            script.Attributes["data-origin"] = "password-validator";

            var jsUrl = UrlHelper.Content("~/js/smartstore.passwordvalidator.js");
            script.InnerHtml.AppendHtml($"import {{ PasswordValidator }} from '{jsUrl}';\nnew PasswordValidator('#{id}');");

            var widgetProvider = ViewContext.HttpContext.RequestServices.GetRequiredService<IWidgetProvider>();
            widgetProvider.RegisterWidget("scripts", new HtmlWidget(script));
        }

        private PasswordPolicy GetPasswordPolicy()
        {
            var settings = CustomerSettings;
            return new PasswordPolicy
            {
                MinLength = MinLength ?? settings?.PasswordMinLength ?? 0,
                RequireLower = RequireLower ?? settings?.PasswordRequireLowercase ?? false,
                RequireUpper = RequireUpper ?? settings?.PasswordRequireUppercase ?? false,
                RequireDigit = RequireDigit ?? settings?.PasswordRequireDigit ?? false,
                RequireNonAlpha = RequireNonAlpha ?? settings?.PasswordRequireNonAlphanumeric ?? false,
                UniqueChars = UniqueChars ?? settings?.PasswordRequiredUniqueChars ?? 0
            };
        }

        private static TagBuilder RenderRule(string key, LocalizedString message)
        {
            var li = new TagBuilder("li");
            li.AddCssClass("pwd-rule");
            li.Attributes["data-rule"] = key;
            li.Attributes["data-msg"] = message;

            var faLi = new TagBuilder("span");
            faLi.AddCssClass("fa-li");

            var icon = new TagBuilder("i");
            icon.AppendCssClass("fa fa-ban rule-icon");
            icon.Attributes["aria-hidden"] = "true";

            faLi.InnerHtml.AppendHtml(icon);
            li.InnerHtml.AppendHtml(faLi);
            li.InnerHtml.AppendHtml((IHtmlContent)message);

            return li;
        }
    }
}
