using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Components;

namespace Smartstore.Web.Areas.Admin.Components
{
    public class AdminNavbarViewComponent : SmartViewComponent
    {
        private readonly CustomerSettings _customerSettings;

        public AdminNavbarViewComponent(CustomerSettings customerSettings)
        {
            _customerSettings = customerSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;

            ViewBag.UserName = _customerSettings.CustomerLoginType != CustomerLoginType.Email ? currentCustomer.Username : currentCustomer.Email;
            ViewBag.Stores = Services.StoreContext.GetAllStores();
            if (await Services.Permissions.AuthorizeAsync(Permissions.System.Maintenance.Read))
            {
                // TODO: (mh) (core) CheckUpdateResult: implement and uncomment later
                //ViewBag.CheckUpdateResult = AsyncRunner.RunSync(() => CheckUpdateInternalAsync(false));
            }

            return View();
        }
    }
}
