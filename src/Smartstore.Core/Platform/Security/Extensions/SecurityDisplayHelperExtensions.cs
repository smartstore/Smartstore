using System;
using Smartstore.Core.Security;

namespace Smartstore
{
    public static class SecurityDisplayHelperExtensions
    {
        public static bool HoneypotProtectionEnabled(this IDisplayHelper displayHelper)
        {
            return displayHelper.HttpContext.GetItem(nameof(HoneypotProtectionEnabled), () => 
            {
                return displayHelper.Resolve<SecuritySettings>().EnableHoneypotProtection;
            });
        }

        public static bool IsStoreClosed(this IDisplayHelper displayHelper)
        {
            return displayHelper.HttpContext.GetItem(nameof(IsStoreClosed), () =>
            {
                // TODO: (core) Implement SecurityDisplayHelperExtensions.IsStoreClosed()
                //var settings = displayHelper.Resolve<StoreInformationSettings>;
                //return IsAdmin() && settings.StoreClosedAllowForAdmins ? false : settings.StoreClosed;

                return false;
            });
        }
    }
}
