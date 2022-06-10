global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;
global using Smartstore.Core.Localization;
global using Smartstore.Web.Modelling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Google.Analytics.Components;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Http;

namespace Smartstore.Google.Analytics
{
    internal class Module : ModuleBase, IConfigurable, IWidget, ICookiePublisher
    {
        private readonly GoogleAnalyticsSettings _googleAnalyticsSettings;
        private readonly IProviderManager _providerManager;
        private readonly WidgetSettings _widgetSettings;

        public Module(
            GoogleAnalyticsSettings googleAnalyticsSettings,         
            IProviderManager providerManager,
            WidgetSettings widgetSettings)
        {
            _googleAnalyticsSettings = googleAnalyticsSettings;
            _providerManager = providerManager;
            _widgetSettings = widgetSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "GoogleAnalytics", new { area = "Admin" });

        public Task<IEnumerable<CookieInfo>> GetCookieInfoAsync()
        {
            var widget = _providerManager.GetProvider<IWidget>("Smartstore.Google.Analytics");
            if (!widget.IsWidgetActive(_widgetSettings))
                return Task.FromResult(Enumerable.Empty<CookieInfo>());

            var cookieInfo = new CookieInfo
            {
                Name = T("Plugins.FriendlyName.SmartStore.GoogleAnalytics"),
                Description = T("Plugins.Widgets.GoogleAnalytics.CookieInfo"),
                CookieType = CookieType.Analytics
            };

            return Task.FromResult<IEnumerable<CookieInfo>>(new CookieInfo[] { cookieInfo });
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
                TrackingScript = AnalyticsScriptUtility.GetTrackingScript(),
                EcommerceScript = AnalyticsScriptUtility.GetEcommerceScript(),
                EcommerceDetailScript = AnalyticsScriptUtility.GetEcommerceDetailScript()
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