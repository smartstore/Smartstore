global using System;
global using System.Collections.Generic;
global using System.IO;
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
        public static string PartnerId => "D9X8D9DSNFZBU";
        public static string PartnerClientId => "AbTCZEJtQBJTYwXMK5W4p0-WDfKx9wEoT0xdMAF7OldhDF36aK5k1tD-9eW3LKBE7CKpyilyBh-VDlMF";

        // TODO: (mh) (core) What about cookies?
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
