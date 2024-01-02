using System.Text;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Widgets
{
    public partial class WidgetService : IWidgetService, IWidgetSource
    {
        const string WIDGETS_ALLMETADATA_KEY = "widgets:allmetadata";
        private readonly static CompositeFormat WIDGETS_ACTIVE_KEY = CompositeFormat.Parse("Smartstore.widgets.active-{0}");
        private readonly static CompositeFormat WIDGETS_BYZONE_KEY = CompositeFormat.Parse("Smartstore.widgets.byzone-{0}-{1}");

        private readonly WidgetSettings _widgetSettings;
        private readonly IProviderManager _providerManager;
        private readonly ICacheFactory _cacheFactory;
        private readonly ISettingFactory _settingFactory;
        private readonly IRequestCache _requestCache;
        private readonly IStoreContext _storeContext;

        public WidgetService(
            WidgetSettings widgetSettings,
            IProviderManager providerManager,
            ICacheFactory cacheFactory,
            ISettingFactory settingFactory,
            IRequestCache requestCache,
            IStoreContext storeContext)
        {
            _widgetSettings = widgetSettings;
            _providerManager = providerManager;
            _cacheFactory = cacheFactory;
            _settingFactory = settingFactory;
            _requestCache = requestCache;
            _storeContext = storeContext;
        }

        #region IWidgetSource

        int IWidgetSource.Order { get; } = -1000;

        Task<IEnumerable<Widget>> IWidgetSource.GetWidgetsAsync(string zone, bool isPublicArea, object model)
        {
            var widgets = Enumerable.Empty<Widget>();

            if (isPublicArea)
            {
                var storeId = _storeContext.CurrentStore.Id;
                widgets = LoadActiveWidgetsByWidgetZone(zone, storeId)
                    .Select(x => x.Value.GetDisplayWidget(zone, model, storeId))
                    .Where(x => x != null);
            }

            return Task.FromResult(widgets);
        }

        #endregion

        public virtual IEnumerable<Provider<IActivatableWidget>> LoadActiveWidgets(int storeId = 0)
        {
            var activeWidgets = _requestCache.Get(WIDGETS_ACTIVE_KEY.FormatInvariant(storeId), () =>
            {
                var allWigets = _providerManager.GetAllProviders<IActivatableWidget>(storeId);
                return allWigets.Where(p => _widgetSettings.ActiveWidgetSystemNames.Contains(p.Metadata.SystemName, StringComparer.InvariantCultureIgnoreCase)).ToList();
            });

            return activeWidgets;
        }

        public virtual IEnumerable<Provider<IActivatableWidget>> LoadActiveWidgetsByWidgetZone(string widgetZone, int storeId = 0)
        {
            var widgets = Enumerable.Empty<Provider<IActivatableWidget>>();

            if (widgetZone.IsEmpty())
            {
                return widgets;
            }

            var map = GetWidgetMetadataMap();

            if (map.TryGetValues(widgetZone, out var widgetMetadatas))
            {
                widgets = _requestCache.Get(WIDGETS_BYZONE_KEY.FormatInvariant(widgetZone.ToLower(), storeId), () =>
                {
                    var providers = widgetMetadatas
                        .Where(m => _widgetSettings.ActiveWidgetSystemNames.Contains(m.SystemName, StringComparer.OrdinalIgnoreCase))
                        .Select(m => _providerManager.GetProvider<IActivatableWidget>(m.SystemName, storeId))
                        .Where(x => x != null)
                        .ToList();

                    return providers;
                });
            }

            return widgets;
        }

        public virtual async Task ActivateWidgetAsync(string systemName, bool activate)
        {
            Guard.NotNull(systemName);

            var widget = _providerManager.GetProvider<IActivatableWidget>(systemName);
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
                var widgets = _providerManager.GetAllProviders<IActivatableWidget>(0);
                var map = new Multimap<string, ProviderMetadata>(StringComparer.OrdinalIgnoreCase);

                foreach (var widget in widgets)
                {
                    var zones = widget.Value.GetWidgetZones();
                    if (zones != null)
                    {
                        for (var i = 0; i < zones.Length; i++)
                        {
                            map.Add(zones[i], widget.Metadata);
                        }
                    }
                }

                return map;
            });
        }
    }
}
