// TODO: (mh) (core) Global usings
using System.Threading.Tasks;
using Smartstore.Engine.Modularity;
using Smartstore.PayPal.Settings;

namespace Smartstore.PayPal
{
    internal class Module : ModuleBase
    {
        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySaveSettingsAsync<PayPalSettings>();
            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();
            await DeleteSettingsAsync<PayPalSettings>();
            await base.UninstallAsync();
        }
    }
}
