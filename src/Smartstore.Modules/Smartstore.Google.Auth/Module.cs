using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Google.Auth.Components;

namespace Smartstore.Google.Auth
{
    internal class Module : ModuleBase, IConfigurable, IWidget
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "GoogleAuth", new { area = "Admin" });

        public WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId)
            => new ComponentWidgetInvoker(typeof(GoogleAuthViewComponent), null);

        public string[] GetWidgetZones()
            => new string[] { "external_auth_buttons" };

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();
            await base.UninstallAsync();
        }
    }
}
