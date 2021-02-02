using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Content.Widgets
{
    public partial class WidgetService : IWidgetService
    {
        private const string WIDGETS_ACTIVE_KEY = "SmartStore.widgets.active-{0}";
        private const string WIDGETS_ZONEMAPPED_KEY = "SmartStore.widgets.zonemapped-{0}";

        private readonly WidgetSettings _widgetSettings;
        private readonly IProviderManager _providerManager;
        private readonly IRequestCache _requestCache;

        public WidgetService(
            WidgetSettings widgetSettings,
            IProviderManager providerManager,
            IRequestCache requestCache)
        {
            _widgetSettings = widgetSettings;
            _providerManager = providerManager;
            _requestCache = requestCache;
        }

        /// <summary>
        /// Load active widgets.
        /// </summary>
        /// <param name="storeId">Load records allows only in specified store; pass 0 to load all records.</param>
        /// <returns>Active widgets.</returns>
        public virtual IEnumerable<Provider<IWidget>> LoadActiveWidgets(int storeId = 0)
        {
            var activeWidgets = _requestCache.Get(WIDGETS_ACTIVE_KEY.FormatInvariant(storeId), () =>
            {
                var allWigets = _providerManager.GetAllProviders<IWidget>(storeId);
                return allWigets.Where(p => _widgetSettings.ActiveWidgetSystemNames.Contains(p.Metadata.SystemName, StringComparer.InvariantCultureIgnoreCase)).ToList();
            });

            return activeWidgets;
        }

        /// <summary>
        /// Load active widgets by widget zone.
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="storeId">Load records allows only in specified store; pass 0 to load all records</param>
        /// <returns>Widgets</returns>
        public virtual IEnumerable<Provider<IWidget>> LoadActiveWidgetsByWidgetZone(string widgetZone, int storeId = 0)
        {
            if (widgetZone.IsEmpty())
                return Enumerable.Empty<Provider<IWidget>>();

            var mappedWidgets = _requestCache.Get(WIDGETS_ZONEMAPPED_KEY.FormatInvariant(storeId), () =>
            {
                var activeWidgets = LoadActiveWidgets(storeId);
                var map = new Multimap<string, Provider<IWidget>>();

                foreach (var widget in activeWidgets)
                {
                    var zones = widget.Value.GetWidgetZones();
                    if (zones != null && zones.Any())
                    {
                        foreach (var zone in zones.Select(x => x.ToLower()))
                        {
                            map.Add(zone, widget);
                        }
                    }
                }

                return map;
            });

            widgetZone = widgetZone.ToLower();

            if (mappedWidgets.ContainsKey(widgetZone))
            {
                return mappedWidgets[widgetZone];
            }

            return Enumerable.Empty<Provider<IWidget>>();
        }
    }
}
