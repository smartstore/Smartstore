using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Identity;

namespace Smartstore.Web.TagHelpers.Public
{
    [HtmlTargetElement("script", Attributes = ConsentTypeAttributeName)]
    public class ConsentScriptTagHelper(ICookieConsentManager cookieConsentManager) : TagHelper
    {
        const string ConsentTypeAttributeName = "sm-consent-type";
        const string SrcAttributeName = "src";

        private readonly ICookieConsentManager _cookieConsentManager = cookieConsentManager;

        public override int Order => int.MinValue;

        /// <summary>
        /// The type of consent that is required for the script to be loaded.
        /// </summary>
        [HtmlAttributeName(ConsentTypeAttributeName)]
        public CookieType ConsentType { get; set; }

        /// <summary>
        /// The source of the script. If the source is not set, we assume the script is inline.
        /// </summary>
        [HtmlAttributeName(SrcAttributeName)]
        public string Src { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);

            var isAllowed = await _cookieConsentManager.IsCookieAllowedAsync(ConsentType);
            if (!isAllowed)
            {
                if (Src.HasValue())
                {
                    output.Attributes.SetAttribute("data-scr", Src);
                    output.Attributes.RemoveAll("scr");
                }
                else
                {
                    output.Attributes.SetAttribute("type", "text/plain");
                }

                output.Attributes.Add("data-consent", ConsentType.ToString().ToLower());
            }
            else if (Src.HasValue())
            {
                output.Attributes.SetAttribute("scr", Src);
            }
        }
    }
}