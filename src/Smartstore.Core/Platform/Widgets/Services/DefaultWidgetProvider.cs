using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Smartstore.Collections;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetProvider : IWidgetProvider
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IApplicationContext _appContext;
        private readonly IMemoryCache _memoryCache;

        private Multimap<string, WidgetInvoker> _zoneWidgetsMap;
        private Multimap<Regex, WidgetInvoker> _zoneExpressionWidgetsMap;

        public DefaultWidgetProvider(IHttpContextAccessor accessor, IApplicationContext appContext, IMemoryCache memoryCache)
        {
            _accessor = accessor;
            _appContext = appContext;
            _memoryCache = memoryCache;
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
                _zoneWidgetsMap = new Multimap<string, WidgetInvoker>(StringComparer.OrdinalIgnoreCase, invokers => new HashSet<WidgetInvoker>());
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
                _zoneExpressionWidgetsMap = new Multimap<Regex, WidgetInvoker>(invokers => new HashSet<WidgetInvoker>());
            }

            _zoneExpressionWidgetsMap.Add(zonePattern, widget);
        }

        public IEnumerable<WidgetInvoker> GetWidgets(string zone)
        {
            if (zone.IsEmpty())
            {
                yield break;
            }

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.TryGetValues(zone, out var widgets))
            {
                foreach (var widget in widgets)
                {
                    yield return widget;
                }
            }

            if (_zoneExpressionWidgetsMap != null)
            {
                foreach (var entry in _zoneExpressionWidgetsMap)
                {
                    var rg = entry.Key;
                    if (rg.IsMatch(zone))
                    {
                        foreach (var widget in entry.Value)
                        {
                            yield return widget;
                        }
                    }
                }
            }
        }

        public bool HasWidgets(string zone)
        {
            return GetWidgets(zone).Any();
        }

        public bool ContainsWidget(string zone, string widgetKey)
        {
            return GetWidgets(zone).Any(x => x.Key == widgetKey);
        }

        //public async Task<bool> HasContentAsync(string zone, ViewContext viewContext)
        //{
        //    Guard.NotNull(viewContext, nameof(viewContext));
            
        //    var widgets = GetWidgets(zone);

        //    foreach (var widget in widgets)
        //    {
        //        var content = await widget.InvokeAsync(viewContext);
                
        //    }
        //}

        public async Task<dynamic> GetAllKnownWidgetZonesAsync()
        {
            var fileName = "widgetzones.json";
            var fs = _appContext.AppDataRoot;
            var cacheKey = _memoryCache.BuildScopedKey(fileName);

            var rawJson = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                if (fs.FileExists(fileName))
                {
                    entry.ExpirationTokens.Add(fs.Watch(fileName));
                    return await fs.ReadAllTextAsync(fileName);
                }
                else
                {
                    return string.Empty;
                }
            });

            if (rawJson is string json && json.HasValue())
            {
                try
                {
                    return JObject.Parse(json);
                }
                catch
                {
                    // Json is invalid. Don't parse again.
                    _memoryCache.Set(cacheKey, string.Empty);
                }
            }

            return null;
        }
    }
}