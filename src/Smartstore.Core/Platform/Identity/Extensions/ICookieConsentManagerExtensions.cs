using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Identity
{
    public static class ICookieConsentManagerExtensions
    {
        /// <inheritdoc cref="ICookieConsentManager.GenerateScript(bool, CookieType, string)" />
        public static async Task<TagBuilder> GenerateScriptAsync(this ICookieConsentManager manager, CookieType consentType, string src)
        {
            return manager.GenerateScript(await manager.IsCookieAllowedAsync(consentType), consentType, src);
        }

        /// <inheritdoc cref="ICookieConsentManager.GenerateInlineScript(bool, CookieType, string)" />
        public static async Task<TagBuilder> GenerateInlineScriptAsync(this ICookieConsentManager manager, CookieType consentType, string code)
        {
            return manager.GenerateInlineScript(await manager.IsCookieAllowedAsync(consentType), consentType, code);
        }
    }
}
