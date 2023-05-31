using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetSelector : IWidgetSelector
    {
        private readonly static string[] StartAliases = new[] { "body_start_html_tag_after", "head_html_tag" };
        private readonly static string[] EndAliases = new[] { "body_end_html_tag_before" };

        private readonly IWidgetSource[] _widgetSources;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultWidgetSelector(IEnumerable<IWidgetSource> widgetSources, IHttpContextAccessor httpContextAccessor)
        {
            _widgetSources = widgetSources.OrderBy(x => x.Order).ToArray();
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<Widget>> GetWidgetsAsync(string zone, object model = null)
        {
            Guard.NotEmpty(zone);

            var httpContext = _httpContextAccessor.HttpContext;
            var isPublicArea = httpContext != null && httpContext.GetRouteData().Values.GetAreaName().IsEmpty();
            var zoneAliases = GetZoneAliases(zone);
            var widgets = Enumerable.Empty<Widget>();

            for (var i = 0; i < _widgetSources.Length; i++)
            {
                var localWidgets = await _widgetSources[i].GetWidgetsAsync(zone, isPublicArea, model);
                if (localWidgets != null)
                {
                    widgets = widgets.Concat(localWidgets);
                }

                if (zoneAliases != null)
                {
                    for (var y = 0; y < zoneAliases.Length; y++)
                    {
                        var legacyWidgets = await _widgetSources[i].GetWidgetsAsync(zoneAliases[y], isPublicArea, model);
                        if (legacyWidgets != null)
                        {
                            widgets = widgets.Concat(legacyWidgets);
                        }
                    }
                }
            }

            if (widgets.Any())
            {
                widgets = widgets
                    .Distinct()
                    .OrderBy(x => x.Prepend)
                    .ThenBy(x => x.Order);
            }

            return widgets;
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