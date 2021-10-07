using Microsoft.AspNetCore.Mvc;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    public class PmAccountDropdownViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(PmAccountDropdownModel model)
        {
            return View(model);
        }
    }
}
