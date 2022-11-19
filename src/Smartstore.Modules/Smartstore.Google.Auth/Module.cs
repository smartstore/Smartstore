global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Authentication.Google;
global using Microsoft.Extensions.Options;
global using Smartstore.Web.Modelling;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Google.Auth.Components;
using Smartstore.Http;

namespace Smartstore.Google.Auth
{
    internal class Module : ModuleBase, IConfigurable, IExternalAuthenticationMethod
    {
        public RouteInfo GetConfigurationRoute()
            => new("Configure", "GoogleAuth", new { area = "Admin" });

        public Widget GetDisplayWidget(int storeId)
            => new ComponentWidget(typeof(GoogleAuthViewComponent), null);

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
