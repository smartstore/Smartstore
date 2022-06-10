using Smartstore.Web.Models;

namespace Smartstore.Admin.Controllers
{
    public class SecurityController : AdminController
    {
        // INFO: instead, throw new AccessDeniedException()

        //[ValidateAdminIpAddress(false)]
        //public IActionResult AccessDenied(string pageUrl)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;

        //    if (customer == null || customer.IsGuest())
        //    {
        //        Logger.Info(T("Admin.System.Warnings.AccessDeniedToAnonymousRequest", pageUrl.NaIfEmpty()));
        //        return View();
        //    }

        //    Logger.Info(T("Admin.System.Warnings.AccessDeniedToUser",
        //        customer.Email.NaIfEmpty(), 
        //        customer.Email.NaIfEmpty(), 
        //        pageUrl.NaIfEmpty()));

        //    return View();
        //}

        /// <summary>
        /// Called by AJAX
        /// </summary>
        public async Task<IActionResult> AllAccessPermissions(string selected)
        {
            var systemNames = await Services.Permissions.GetAllSystemNamesAsync();
            var selectedArr = selected.SplitSafe(',');

            var data = systemNames
                .Select(x => new ChoiceListItem
                {
                    Id = x.Key,
                    Text = x.Value,
                    Selected = selectedArr.Contains(x.Key)
                })
                .ToList();

            return Json(data);
        }
    }
}
