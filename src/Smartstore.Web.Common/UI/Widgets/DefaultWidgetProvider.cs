using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Smartstore.Collections;

namespace Smartstore.Web.UI
{
    public class DefaultWidgetProvider : IWidgetProvider
    {
        private readonly IHttpContextAccessor _accessor;

        private Multimap<string, WidgetInvoker> _zoneWidgetsMap;
        private Multimap<Regex, WidgetInvoker> _zoneExpressionWidgetsMap;

        public DefaultWidgetProvider(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public virtual void RegisterWidget(string[] zones, WidgetInvoker widget)
        {
            Guard.NotNull(zones, nameof(zones));
            Guard.NotNull(widget, nameof(widget));

            if (_accessor.HttpContext?.Request?.Query?.ContainsKey("nowidgets") == true)
            {
                return;
            }

            if (_zoneWidgetsMap == null)
            {
                _zoneWidgetsMap = new Multimap<string, WidgetInvoker>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var zone in zones)
            {
                _zoneWidgetsMap.Add(zone, widget);
            }
        }

        public virtual void RegisterWidget(Regex zonePattern, WidgetInvoker widget)
        {
            Guard.NotNull(zonePattern, nameof(zonePattern));
            Guard.NotNull(widget, nameof(widget));

            if (_accessor.HttpContext?.Request?.Query?.ContainsKey("nowidgets") == true)
            {
                return;
            }

            if (_zoneExpressionWidgetsMap == null)
            {
                _zoneExpressionWidgetsMap = new Multimap<Regex, WidgetInvoker>();
            }

            _zoneExpressionWidgetsMap.Add(zonePattern, widget);
        }

        public IEnumerable<WidgetInvoker> GetWidgets(string zone)
        {
            if (zone.IsEmpty())
            {
                return Enumerable.Empty<WidgetInvoker>();
            } 

            var result = new List<WidgetInvoker>();

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.ContainsKey(zone))
            {
                result.AddRange(_zoneWidgetsMap[zone]);
            }

            if (_zoneExpressionWidgetsMap != null)
            {
                foreach (var entry in _zoneExpressionWidgetsMap)
                {
                    var rg = entry.Key;
                    if (rg.IsMatch(zone))
                    {
                        result.AddRange(entry.Value);
                    }
                }
            }

            return result;
        }
    }
}