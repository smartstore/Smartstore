using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Smartstore.Core.Widgets;
using Smartstore.Web.Components;

namespace Smartstore.DevTools.Components
{
    public class WidgetZoneViewComponent : SmartViewComponent
    {
        private readonly IWidgetProvider _widgetProvider;

        public WidgetZoneViewComponent(IWidgetProvider widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get widget zone areas.
            var jsonZones = (JObject)(await _widgetProvider.GetAllKnownWidgetZonesAsync());

            var areas =
                from p in jsonZones["WidgetZonesAreas"]
                select p;

            // Localize widget zone areas.
            foreach (var area in areas)
            {
                var areaRessource = area["name"].ToString();
                var areaName = T(areaRessource);

                area["name"] = areaName.Value;
            }

            ViewBag.WidgetZoneAreas = areas;

            return View();
        }
    }
}