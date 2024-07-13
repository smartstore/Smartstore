using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Identity;

namespace Smartstore
{
    public static class CookieDisplayHelperExtensions
    {
        public static Task<bool> IsCookieAllowedAsync(this IDisplayHelper displayHelper, CookieType type)
        {
            var cookieConsentManager = displayHelper.Resolve<ICookieConsentManager>();
            return cookieConsentManager.IsCookieAllowedAsync(type);
        }

        public static TagBuilder ConsentableScript(this IHtmlHelper htmlHelper, bool consented, CookieType consentType, string src)
        {
            // TODO: (mh) Call this method where applicable (instead of the ugly string concat mess)
            Guard.NotEmpty(src);
            
            var script = new TagBuilder("script");
            if (consented)
            {
               script.Attributes["src"] = src;
            }
            else
            {
                script.Attributes["data-src"] = src;
                // TODO: (mh) Check casing/dasherization
                script.Attributes["data-consent"] = consentType.ToString().ToLowerInvariant();
            }

            return script;
        }

        public static TagBuilder ConsentableInlineScript(this IHtmlHelper htmlHelper, bool consented, CookieType consentType, string code)
        {
            // TODO: (mh) Call this method where applicable (instead of the ugly string concat mess)
            Guard.NotEmpty(code);

            var script = new TagBuilder("script");
            script.InnerHtml.AppendHtml(code);

            if (!consented)
            {
                script.Attributes["type"] = "text/plain";
                // TODO: (mh) Check casing/dasherization
                script.Attributes["data-consent"] = consentType.ToString().ToLowerInvariant();
            }

            return script;
        }
    }
}
