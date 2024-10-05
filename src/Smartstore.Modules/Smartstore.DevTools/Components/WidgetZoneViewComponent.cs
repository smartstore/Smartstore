using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Smartstore.Core.Widgets;
using Smartstore.Web.Components;

namespace Smartstore.DevTools.Components
{
    public class WidgetZoneViewComponent : SmartViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(bool renderMenu = false)
        {
            if (!renderMenu)
            {
                // Zone preview rendering
                return View();
            }
            
            // Menu rendering
            var widgetProvider = HttpContext.RequestServices.GetRequiredService<IWidgetProvider>();

            // Get widget zone areas.
            var jsonZones = (JObject)(await widgetProvider.GetAllKnownWidgetZonesAsync());

            var groups =
                from p in jsonZones["WidgetZonesAreas"]
                select p;

            // Localize widget zone areas.
            foreach (var group in groups)
            {
                var areaRessource = group["name"].ToString();
                var areaName = T(areaRessource);

                group["name"] = areaName.Value;
            }

            ViewBag.WidgetZoneGroups = groups;

            return View("Menu");
        }
    }
}