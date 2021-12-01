using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.GoogleAnalytics.Components;
using Smartstore.GoogleAnalytics.Settings;
using Smartstore.Http;

namespace Smartstore.GoogleAnalytics
{
    internal class Module : ModuleBase, IConfigurable, IWidget
    {
        private readonly GoogleAnalyticsSettings _googleAnalyticsSettings;

        public Module(GoogleAnalyticsSettings googleAnalyticsSettings)
        {
            _googleAnalyticsSettings = googleAnalyticsSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "GoogleAnalytics", new { area = "Admin" });

        public WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId)
            => new ComponentWidgetInvoker(typeof(GoogleAnalyticsViewComponent), null);

        public string[] GetWidgetZones()
        {
            var zones = new string[] { "head" };

            if (_googleAnalyticsSettings.WidgetZone.HasValue())
            {
                zones = new string[] { _googleAnalyticsSettings.WidgetZone };
            }

            return zones;
        }

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync<GoogleAnalyticsSettings>();
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
