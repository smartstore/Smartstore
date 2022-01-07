using Smartstore.Core.Identity;
using Smartstore.Core.Packaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Admin.Components
{
    public class AdminNavbarViewComponent : SmartViewComponent
    {
        private readonly CustomerSettings _customerSettings;
        private readonly Lazy<UpdateChecker> _updateChecker;

        public AdminNavbarViewComponent(CustomerSettings customerSettings, Lazy<UpdateChecker> updateChecker)
        {
            _customerSettings = customerSettings;
            _updateChecker = updateChecker;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;

            ViewBag.UserName = _customerSettings.CustomerLoginType != CustomerLoginType.Email ? currentCustomer.Username : currentCustomer.Email;
            ViewBag.Stores = Services.StoreContext.GetAllStores();
            if (await Services.Permissions.AuthorizeAsync(Permissions.System.Maintenance.Read))
            {
                ViewBag.CheckUpdateResult = await _updateChecker.Value.CheckUpdateAsync(false);
            }

            return View();
        }
    }
}
