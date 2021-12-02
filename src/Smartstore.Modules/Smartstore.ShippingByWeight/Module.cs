using System.Threading.Tasks;
using Smartstore.Engine.Modularity;
using Smartstore.ShippingByWeight.Settings;

namespace Smartstore.ShippingByWeight
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync<ShippingByWeightSettings>();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            // TODO: (mh) (core) Flush tables???
            await DeleteLanguageResourcesAsync();
            await DeleteSettingsAsync<ShippingByWeightSettings>();
            await base.UninstallAsync();
        }
    }
}
