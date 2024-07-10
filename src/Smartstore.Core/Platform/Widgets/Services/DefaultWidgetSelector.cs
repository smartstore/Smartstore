using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetSelector : IWidgetSelector
    {
        private readonly static string[] StartAliases = ["body_start_html_tag_after", "head_html_tag"];
        private readonly static string[] EndAliases = ["body_end_html_tag_before"];

        private readonly IWidgetSource[] _widgetSources;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultWidgetSelector(IEnumerable<IWidgetSource> widgetSources, IHttpContextAccessor httpContextAccessor)
        {
            _widgetSources = widgetSources.OrderBy(x => x.Order).ToArray();
            _httpContextAccessor = httpContextAccessor;
        }

        public virtual async IAsyncEnumerable<Widget> EnumerateWidgetsAsync(IWidgetZone zone, object model = null)
        {
            Guard.NotNull(zone);

            var httpContext = _httpContextAccessor.HttpContext;
            var isPublicArea = httpContext != null && httpContext.GetRouteData().Values.GetAreaName().IsEmpty();
            var zoneAliases = GetZoneAliases(zone.Name);

            for (var i = 0; i < _widgetSources.Length; i++)
            {
                var localWidgets = await _widgetSources[i].GetWidgetsAsync(zone, isPublicArea, model);
                if (localWidgets != null)
                {
                    foreach (var widget in localWidgets)
                    {
                        if (widget.IsValid(zone))
                        {
                            yield return widget;
                        }
                    }
                }

                if (zoneAliases != null)
                {
                    var aliasZones = zoneAliases.Select(x => new PlainWidgetZone(zone) { Name = x }).ToArray();
                    for (var y = 0; y < aliasZones.Length; y++)
                    {
                        var legacyWidgets = await _widgetSources[i].GetWidgetsAsync(aliasZones[y], isPublicArea, model);
                        if (legacyWidgets != null)
                        {
                            foreach (var widget in legacyWidgets)
                            {
                                if (widget.IsValid(zone))
                                {
                                    yield return widget;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// For legacy widget name mapping.
        /// </summary>
        private static string[] GetZoneAliases(string zone)
        {
            return zone switch
            {
                "start" => StartAliases,
                "end"   => EndAliases,
                _       => null
            };
        }
    }
}