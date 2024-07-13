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
    }
}
