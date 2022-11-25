global using System;
using System.Threading.Tasks;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.StripeElements.Settings;

namespace Smartstore.StripeElements
{
    internal class Module : ModuleBase, IConfigurable
    {
        public RouteInfo GetConfigurationRoute()
            => new("Configure", "StripeAdmin", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync<StripeSettings>();

            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();
            await DeleteSettingsAsync<StripeSettings>();

            await base.UninstallAsync();
        }
    }
}