using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetSelector : IWidgetSelector
    {
        private readonly static Dictionary<string, string> _legacyWidgetNameMap = new()
        {
            { "body_start_html_tag_after", "start" },
            { "body_end_html_tag_before", "end" },
            { "head_html_tag", "start" }
        };

        private readonly IWidgetSource[] _widgetSources;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultWidgetSelector(IEnumerable<IWidgetSource> widgetSources, IHttpContextAccessor httpContextAccessor)
        {
            _widgetSources = widgetSources.OrderBy(x => x.Order).ToArray();
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<Widget>> GetWidgetsAsync(string zone, object model = null)
        {
            Guard.NotEmpty(zone, nameof(zone));

            if (_legacyWidgetNameMap.ContainsKey(zone))
            {
                zone = _legacyWidgetNameMap[zone];
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var isPublicArea = httpContext != null && httpContext.GetRouteData().Values.GetAreaName().IsEmpty();
            var widgets = Enumerable.Empty<Widget>();

            for (var i = 0; i < _widgetSources.Length; i++)
            {
                var localWidgets = await _widgetSources[i].GetWidgetsAsync(zone, isPublicArea, model);
                if (localWidgets != null)
                {
                    widgets = widgets.Concat(localWidgets);
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
    }
}