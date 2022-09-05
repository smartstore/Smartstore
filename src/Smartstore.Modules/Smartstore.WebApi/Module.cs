global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Smartstore.Web.Modelling;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.WebApi
{
    // TODO: (mg) (core) cleanup string resources.
    internal class Module : ModuleBase, IConfigurable
    {
        public RouteInfo GetConfigurationRoute()
            => new("Configure", "WebApi", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            //await TrySaveSettingsAsync<WebApiSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            //await DeleteSettingsAsync<WebApiSettings>();
            await DeleteLanguageResourcesAsync();
            await base.UninstallAsync();
        }
    }
}
