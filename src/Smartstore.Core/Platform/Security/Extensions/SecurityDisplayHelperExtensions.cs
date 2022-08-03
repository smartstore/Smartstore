using Smartstore.Core;
using Smartstore.Core.Common.Settings;
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
                var settings = displayHelper.Resolve<StoreInformationSettings>();
                var customer = displayHelper.Resolve<IWorkContext>().CurrentCustomer;

                return settings.StoreClosedAllowForAdmins && customer.IsAdmin()
                    ? false
                    : settings.StoreClosed;
            });
        }
    }
}
