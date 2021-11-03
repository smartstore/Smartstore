using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Microsoft.Auth.Components;
using Smartstore.Http;
using Smartstore.Core.Identity;

namespace Smartstore.Microsoft.Auth
{
    internal class Module : ModuleBase, IConfigurable, IExternalAuthenticationMethod
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "MicrosoftAuth", new { area = "Admin" });

        public WidgetInvoker GetDisplayWidget(int storeId)
            => new ComponentWidgetInvoker(typeof(MicrosoftAuthViewComponent), null);

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
