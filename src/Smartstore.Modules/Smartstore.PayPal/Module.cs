global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading.Tasks;
global using Newtonsoft.Json;
global using Smartstore.Core.Localization;
global using Smartstore.PayPal.Models;
global using Smartstore.PayPal.Settings;
global using Smartstore.Web.Modelling;
using Smartstore.Engine.Modularity;

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