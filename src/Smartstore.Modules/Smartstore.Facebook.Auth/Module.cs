using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Facebook.Auth.Components;
using Smartstore.Http;
using Smartstore.Core.Identity;

namespace Smartstore.Facebook.Auth
{
    internal class Module : ModuleBase, IConfigurable, IWidget, IExternalAuthenticationMethod
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "FacebookAuth", new { area = "Admin" });

        public WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId)
            => new ComponentWidgetInvoker(typeof(FacebookAuthViewComponent), null);

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
