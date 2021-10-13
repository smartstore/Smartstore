using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    /// <summary>
    /// Button to send a private message injected into the customer's profile page.
    /// </summary>
    public class PmButtonViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(int customerId)
        {
            if (customerId == 0)
            {
                return Empty();
            }

            return View(customerId);
        }
    }
}
