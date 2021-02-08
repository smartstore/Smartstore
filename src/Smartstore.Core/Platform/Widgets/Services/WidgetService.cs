using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Widgets
{
    public partial class WidgetService : IWidgetService
    {
        const string WIDGETS_ALLMETADATA_KEY = "widgets:allmetadata";
        const string WIDGETS_ACTIVE_KEY = "Smartstore.widgets.active-{0}";
        const string WIDGETS_ZONEMAPPED_KEY = "Smartstore.widgets.zonemapped-{0}";

        private readonly WidgetSettings _widgetSettings;
        private readonly IProviderManager _providerManager;
        private readonly ICacheManager _cache;
        private readonly IRequestCache _requestCache;

        private static readonly Multimap<(int StoreId, string Zone), Type> _zoneWidgetMap = new();

        public WidgetService(
            WidgetSettings widgetSettings, 
            IProviderManager providerManager,
            ICacheManager cache,
            IRequestCache requestCache)
        {
            _widgetSettings = widgetSettings;
            _providerManager = providerManager;
            _cache = cache;
            _requestCache = requestCache;
        }

        public virtual IEnumerable<Provider<IWidget>> LoadActiveWidgets(int storeId = 0)
        {
            var activeWidgets = _requestCache.Get(WIDGETS_ACTIVE_KEY.FormatInvariant(storeId), () =>
            {
                var allWigets = _providerManager.GetAllProviders<IWidget>(storeId);
                return allWigets.Where(p => _widgetSettings.ActiveWidgetSystemNames.Contains(p.Metadata.SystemName, StringComparer.InvariantCultureIgnoreCase)).ToList();
            });

            return activeWidgets;
        }

        public virtual IEnumerable<Provider<IWidget>> LoadActiveWidgetsByWidgetZone(string widgetZone, int storeId = 0)
        {
            var map = GetWidgetMetadataMap();

            if (widgetZone.HasValue() && map.TryGetValues(widgetZone.ToLower(), out var widgetMetadatas))
            {
                return _requestCache.Get(WIDGETS_ZONEMAPPED_KEY.FormatInvariant(storeId), () =>
                {
                    var providers = widgetMetadatas
                        .Where(m => _widgetSettings.ActiveWidgetSystemNames.Contains(m.SystemName, StringComparer.InvariantCultureIgnoreCase))
                        .Select(m => _providerManager.GetProvider<IWidget>(m.SystemName, storeId))
                        .Where(x => x != null)
                        .ToList();

                    return providers;
                });
            }

            return Enumerable.Empty<Provider<IWidget>>();
        }

        protected virtual Multimap<string, ProviderMetadata> GetWidgetMetadataMap()
        {
            return _cache.Get(WIDGETS_ALLMETADATA_KEY, () =>
            {
                var widgets = _providerManager.GetAllProviders<IWidget>(0);
                var map = new Multimap<string, ProviderMetadata>();

                foreach (var widget in widgets)
                {
                    var zones = widget.Value.GetWidgetZones();
                    if (zones != null && zones.Any())
                    {
                        foreach (var zone in zones.Select(x => x.ToLower()))
                        {
                            map.Add(zone, widget.Metadata);
                        }
                    }
                }

                return map;
            });
        }
    }
}
