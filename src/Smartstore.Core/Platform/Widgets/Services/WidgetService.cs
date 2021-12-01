using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Configuration;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Widgets
{
    public partial class WidgetService : IWidgetService
    {
        const string WIDGETS_ALLMETADATA_KEY = "widgets:allmetadata";
        const string WIDGETS_ACTIVE_KEY = "Smartstore.widgets.active-{0}";
        const string WIDGETS_BYZONE_KEY = "Smartstore.widgets.byzone-{0}-{1}";

        private readonly WidgetSettings _widgetSettings;
        private readonly IProviderManager _providerManager;
        private readonly ICacheFactory _cacheFactory;
        private readonly ISettingFactory _settingFactory;
        private readonly IRequestCache _requestCache;

        public WidgetService(
            WidgetSettings widgetSettings, 
            IProviderManager providerManager,
            ICacheFactory cacheFactory,
            ISettingFactory settingFactory,
            IRequestCache requestCache)
        {
            _widgetSettings = widgetSettings;
            _providerManager = providerManager;
            _cacheFactory = cacheFactory;
            _settingFactory = settingFactory;
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
            if (widgetZone.IsEmpty())
            {
                return Enumerable.Empty<Provider<IWidget>>();
            }

            var map = GetWidgetMetadataMap();

            if (map.TryGetValues(widgetZone.ToLower(), out var widgetMetadatas))
            {
                return _requestCache.Get(WIDGETS_BYZONE_KEY.FormatInvariant(widgetZone, storeId), () =>
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

        public virtual async Task ActivateWidgetAsync(string systemName, bool activate)
        {
            Guard.NotNull(systemName, nameof(systemName));

            var widget = _providerManager.GetProvider<IWidget>(systemName);
            if (widget != null)
            {
                var isActive = widget.IsWidgetActive(_widgetSettings);

                if (isActive && !activate)
                {
                    _widgetSettings.ActiveWidgetSystemNames.Remove(systemName);
                    await _settingFactory.SaveSettingsAsync(_widgetSettings);
                }

                if (!isActive && activate)
                {
                    _widgetSettings.ActiveWidgetSystemNames.Add(systemName);
                    await _settingFactory.SaveSettingsAsync(_widgetSettings);
                }
            }
        }

        protected virtual Multimap<string, ProviderMetadata> GetWidgetMetadataMap()
        {
            return _cacheFactory.GetMemoryCache().Get(WIDGETS_ALLMETADATA_KEY, () =>
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
