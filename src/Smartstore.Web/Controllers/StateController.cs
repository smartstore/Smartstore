using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Controllers
{
    public class StateController : Controller
    {
        [HttpPost, Route("/state/setselectedtab")]
        public IActionResult SetSelectedTab(string navId, string tabId, string path)
        {
            if (navId.HasValue() && tabId.HasValue() && path.HasValue())
            {
                var info = new SelectedTabInfo { TabId = tabId, Path = path };
                TempData["SelectedTab." + navId] = info;
            }

            return Json(new { Success = true });
        }
    }
}
