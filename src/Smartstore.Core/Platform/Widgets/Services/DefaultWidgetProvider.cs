using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Smartstore.Collections;
using Smartstore.Engine;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetProvider : IWidgetProvider
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly ICommonServices _services;
        private readonly IApplicationContext _appContext;

        private Multimap<string, WidgetInvoker> _zoneWidgetsMap;
        private Multimap<Regex, WidgetInvoker> _zoneExpressionWidgetsMap;

        public DefaultWidgetProvider(IHttpContextAccessor accessor, ICommonServices services, IApplicationContext appContext)
        {
            _accessor = accessor;
            _services = services;
            _appContext = appContext;
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

        public bool HasContent(string zone)
        {
            if (zone.IsEmpty())
            {
                return false;
            }

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.ContainsKey(zone))
            {
                return true;
            }

            if (_zoneExpressionWidgetsMap != null)
            {
                foreach (var entry in _zoneExpressionWidgetsMap)
                {
                    var rg = entry.Key;
                    if (rg.IsMatch(zone))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerable<WidgetInvoker> GetWidgets(string zone)
        {
            if (zone.IsEmpty())
            {
                return Enumerable.Empty<WidgetInvoker>();
            } 

            var result = new List<WidgetInvoker>();

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.TryGetValues(zone, out var widgets))
            {
                result.AddRange(widgets);
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

        public bool ContainsWidget(string zone, string widgetKey)
        {
            Guard.NotEmpty(zone, nameof(zone));
            Guard.NotEmpty(widgetKey, nameof(widgetKey));

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.TryGetValues(zone, out var widgets))
            {
                // INFO: Hot path code
                foreach (var widget in widgets)
                {
                    if (widget.Key == widgetKey)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<dynamic> GetAllKnownWidgetZonesAsync()
        {
            var fileName = "widgetzones.json";
            var fs = _appContext.AppDataRoot;

            // TODO: (mh) (core) Use native IMemoryCache with IChangeToken here, not ICacheManager.

            // TODO: (mh) (core) Use BuildScopedKey ?
            //var cacheKey = HttpRuntime.Cache.BuildScopedKey(fileName);
            var cacheKey = "Smartstore:" + fileName;
            var rawJson = await _services.CacheFactory.GetMemoryCache().GetAsync<string>(cacheKey,true);

            if (rawJson == null)
            {
                if (fs.FileExists(fileName))
                {
                    rawJson = await fs.ReadAllTextAsync(fileName);
                    // TODO: (mh) (core) How to get GetCacheDependency ?
                    //var virtualPath = await fs.GetDirectoryAsync(fileName);
                    //var cacheDependency = fs.VirtualPathProvider.GetCacheDependency(virtualPath, DateTime.UtcNow);
                    //await _services.CacheFactory.GetMemoryCache().PutAsync(cacheKey, rawJson, cacheDependency);
                    await _services.CacheFactory.GetMemoryCache().PutAsync(cacheKey, rawJson);
                }
                else
                {
                    await _services.CacheFactory.GetMemoryCache().PutAsync(cacheKey, "");
                }
            }

            if (rawJson is string json && json.HasValue())
            {
                try
                {
                    return JObject.Parse(json);
                }
                catch
                {
                    // Json is invalid. Don't parse again.
                    await _services.CacheFactory.GetMemoryCache().PutAsync(cacheKey, "");
                }
            }

            return null;
        }
    }
}