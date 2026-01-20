using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
            dynamic jsonZones = await widgetProvider.GetAllKnownWidgetZonesAsync();

            var groups = jsonZones?.WidgetZonesAreas as IList<object>;
            if (groups is not null)
            {
                // Localize widget zone areas.
                foreach (var group in groups)
                {
                    if (group is not IDictionary<string, object> obj)
                    {
                        continue;
                    }

                    var areaResource = obj.TryGetValue("name", out var name) ? name?.ToString() : null;
                    if (areaResource.HasValue())
                    {
                        obj["name"] = T(areaResource).Value;
                    }
                }
            }

            ViewBag.WidgetZoneGroups = groups;

            return View("Menu");
        }
    }
}