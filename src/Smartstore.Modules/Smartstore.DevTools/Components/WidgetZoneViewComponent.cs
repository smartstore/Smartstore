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

            return View();
        }
    }
}