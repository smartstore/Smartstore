using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Engine.Modularity;
using Smartstore.Shipping.Settings;

namespace SmartStore.DellyManLogistics
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await SaveSettingsAsync<DellyManLogisticsSettings>();
            await ImportLanguageResourcesAsync();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<DellyManLogisticsSettings>();
            await DeleteLanguageResourcesAsync();
            await base.UninstallAsync();
        }
    }
}
