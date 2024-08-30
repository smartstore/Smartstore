using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Identity;

namespace Smartstore.Web.TagHelpers.Public
{
    [HtmlTargetElement("script", Attributes = ConsentTypeAttributeName)]
    public class ConsentScriptTagHelper(ICookieConsentManager cookieConsentManager) : TagHelper
    {
        const string ConsentTypeAttributeName = "sm-consent-type";

        private readonly ICookieConsentManager _cookieConsentManager = cookieConsentManager;

        public override int Order => int.MinValue;

        /// <summary>
        /// The type of consent that is required for the script to be loaded.
        /// </summary>
        [HtmlAttributeName(ConsentTypeAttributeName)]
        public CookieType ConsentType { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);

            var isAllowed = await _cookieConsentManager.IsCookieAllowedAsync(ConsentType);
            if (!isAllowed)
            {
                // Get src attribute value
                if (context.AllAttributes.TryGetAttribute("src", out var attribute))
                {
                    output.Attributes.SetAttribute("data-src", attribute.Value.ToString());
                    output.Attributes.RemoveAll("src");
                }
                else
                {
                    // If the src attribute is not set, we assume the script is inline and add the type attribute to prevent the script from being executed.
                    output.Attributes.SetAttribute("type", "text/plain");
                }

                output.Attributes.Add("data-consent", ConsentType.ToString().ToLower());
            }
        }
    }
}