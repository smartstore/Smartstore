global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Smartstore.Web.Modelling;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.AmazonPay
{
    [DependentWidgets("Widgets.AmazonPay")]
    [FriendlyName("Amazon Pay")]
    [Order(-1)]
    internal class Module : ModuleBase, IConfigurable
    {
        public static string SystemName => "Smartstore.AmazonPay";

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "AmazonPay", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<AmazonPaySettings>();
            await ImportLanguageResourcesAsync();

            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<AmazonPaySettings>();
            await DeleteLanguageResourcesAsync();

            await base.UninstallAsync();
        }
    }
}
