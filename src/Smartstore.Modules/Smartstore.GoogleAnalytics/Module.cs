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
            return _googleAnalyticsSettings.WidgetZone.HasValue()
                ? new[] { _googleAnalyticsSettings.WidgetZone }
                : new[] { "head" };
        }

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            // TODO (mh) (core) Missing setting initialization
            // TODO (mh) (core) Missing cookie publisher stuff
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
