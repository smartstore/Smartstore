using System.Threading.Tasks;
using Smartstore.Clickatell.Settings;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Clickatell
{
    internal class Module : ModuleBase, IConfigurable
    {
        public RouteInfo GetConfigurationRoute()
            => new("Configure", "Clickatell", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<ClickatellSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<ClickatellSettings>();
            await DeleteLanguageResourcesAsync();
            await base.UninstallAsync();
        }
    }
}
