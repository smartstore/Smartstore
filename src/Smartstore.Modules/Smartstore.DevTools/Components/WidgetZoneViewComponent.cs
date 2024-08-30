using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.DevTools.Components
{
    public class WidgetZoneViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}