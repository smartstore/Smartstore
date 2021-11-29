using System.Threading.Tasks;
using Smartstore.Engine.Modularity;
using Smartstore.Shipping.Settings;

namespace Smartstore.Shipping
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await SaveSettingsAsync<ShippingByTotalSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<ShippingByTotalSettings>();
            await DeleteLanguageResourcesAsync();
            await base.UninstallAsync();
        }
    }
}
