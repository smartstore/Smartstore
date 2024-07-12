using Smartstore.Core.Identity;

namespace Smartstore
{
    public static class CookieDisplayHelperExtensions
    {
        public static bool IsCookieAllowed(this IDisplayHelper displayHelper, CookieType type)
        {
            var cookieConsentManager = displayHelper.Resolve<ICookieConsentManager>();

            return cookieConsentManager.IsCookieAllowedAsync(type).Await();
        }
    }
}
