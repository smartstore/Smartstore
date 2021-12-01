using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.GoogleAnalytics.Components;
using Smartstore.GoogleAnalytics.Settings;
using Smartstore.Http;

namespace Smartstore.GoogleAnalytics
{
    internal class Module : ModuleBase, IConfigurable, IWidget, ICookiePublisher
    {
        private readonly GoogleAnalyticsSettings _googleAnalyticsSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IProviderManager _providerManager;
        private readonly WidgetSettings _widgetSettings;

        public Module(GoogleAnalyticsSettings googleAnalyticsSettings, 
            ILocalizationService localizationService,            
            IProviderManager providerManager,
            WidgetSettings widgetSettings)
        {
            _googleAnalyticsSettings = googleAnalyticsSettings;
            _localizationService = localizationService;
            _providerManager = providerManager;
            _widgetSettings = widgetSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "GoogleAnalytics", new { area = "Admin" });

        public Task<IEnumerable<CookieInfo>> GetCookieInfoAsync()
        {
            var widget = _providerManager.GetProvider<IWidget>("Smartstore.GoogleAnalytics");
            if (!widget.IsWidgetActive(_widgetSettings))
                return null;

            var cookieInfo = new CookieInfo
            {
                Name = _localizationService.GetResource("Plugins.FriendlyName.SmartStore.GoogleAnalytics"),
                Description = _localizationService.GetResource("Plugins.Widgets.GoogleAnalytics.CookieInfo"),
                CookieType = CookieType.Analytics
            };

            return Task.FromResult(new List<CookieInfo> { cookieInfo }.AsEnumerable());
        }

        public WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId)
            => new ComponentWidgetInvoker(typeof(GoogleAnalyticsViewComponent), null);

        public string[] GetWidgetZones()
        {
            return _googleAnalyticsSettings.WidgetZone.HasValue()
                ? new[] { _googleAnalyticsSettings.WidgetZone }
                : new[] { "head" };
        }

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync(new GoogleAnalyticsSettings
            {
                EcommerceDetailScript = AnalyticsScriptUtility.GetEcommerceDetailScript(),
                TrackingScript = AnalyticsScriptUtility.GetTrackingScript(),
                EcommerceScript = AnalyticsScriptUtility.GetEcommerceScript()
            });
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();
            await DeleteSettingsAsync<GoogleAnalyticsSettings>();
            await base.UninstallAsync();
        }
    }
}