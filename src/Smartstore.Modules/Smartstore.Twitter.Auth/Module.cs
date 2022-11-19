global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Authentication.Twitter;
global using Microsoft.Extensions.Options;
global using Smartstore.Web.Modelling;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Twitter.Auth.Components;

namespace Smartstore.Twitter.Auth
{
    internal class Module : ModuleBase, IConfigurable, IExternalAuthenticationMethod
    {
        public RouteInfo GetConfigurationRoute()
            => new("Configure", "TwitterAuth", new { area = "Admin" });

        public Widget GetDisplayWidget(int storeId)
            => new ComponentWidget(typeof(TwitterAuthViewComponent), null);

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
