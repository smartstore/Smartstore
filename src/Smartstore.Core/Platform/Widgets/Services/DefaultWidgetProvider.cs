using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Smartstore.Collections;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetProvider : IWidgetProvider, IWidgetSource
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IApplicationContext _appContext;
        private readonly IMemoryCache _memoryCache;

        private Multimap<string, Widget> _zoneWidgetsMap;
        private Multimap<object, Widget> _zoneExpressionWidgetsMap;

        public DefaultWidgetProvider(IHttpContextAccessor accessor, IApplicationContext appContext, IMemoryCache memoryCache)
        {
            _accessor = accessor;
            _appContext = appContext;
            _memoryCache = memoryCache;
        }

        #region IWidgetSource

        int IWidgetSource.Order { get; } = 1000;

        Task<IEnumerable<Widget>> IWidgetSource.GetWidgetsAsync(IWidgetZone zone, bool isPublicArea, object model)
        {
            return Task.FromResult(GetWidgets(zone));
        }

        #endregion

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

        public virtual void RegisterWidget(string[] zones, Widget widget)
        {
            Guard.NotNull(zones);
            Guard.NotNull(widget);

            if (_accessor.HttpContext?.Request?.Query?.ContainsKey("nowidgets") == true)
            {
                return;
            }

            _zoneWidgetsMap ??= new Multimap<string, Widget>(StringComparer.OrdinalIgnoreCase, invokers => new HashSet<Widget>());

            foreach (var zone in zones)
            {
                _zoneWidgetsMap.Add(zone, widget);
            }
        }

        public virtual void RegisterWidget(Regex zonePattern, Widget widget)
            => RegisterWidgetByExpression(zonePattern, widget);

        public virtual void RegisterWidget(Func<string, bool> zonePredicate, Widget widget)
            => RegisterWidgetByExpression(zonePredicate, widget);

        private void RegisterWidgetByExpression(object expression, Widget widget)
        {
            Guard.NotNull(expression);
            Guard.NotNull(widget);

            if (_accessor.HttpContext?.Request?.Query?.ContainsKey("nowidgets") == true)
            {
                return;
            }

            _zoneExpressionWidgetsMap ??= new Multimap<object, Widget>(invokers => new HashSet<Widget>());
            _zoneExpressionWidgetsMap.Add(expression, widget);
        }

        public bool HasWidgets(IWidgetZone zone)
        {
            return GetWidgets(zone).Any();
        }

        public bool ContainsWidget(IWidgetZone zone, string widgetKey)
        {
            return GetWidgets(zone).Any(x => x.Key == widgetKey);
        }

        public IEnumerable<Widget> GetWidgets(IWidgetZone zone)
        {
            if (zone.Name.IsEmpty())
            {
                yield break;
            }

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.TryGetValues(zone.Name, out var widgets))
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
                    var isMatch = 
                        (entry.Key is Regex rg && rg.IsMatch(zone.Name)) || 
                        (entry.Key is Func<string, bool> fn && fn(zone.Name));
                    
                    if (isMatch)
                    {
                        foreach (var widget in entry.Value)
                        {
                            yield return widget;
                        }
                    }
                }
            }
        }
    }
}