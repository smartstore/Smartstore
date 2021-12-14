global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Smartstore.Web.Modelling;
global using Smartstore.AmazonPay.Domain;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Scheduling;

namespace Smartstore.AmazonPay
{
    [DependentWidgets("Widgets.AmazonPay")]
    [FriendlyName("Amazon Pay")]
    [Order(-1)]
    internal class Module : ModuleBase, IConfigurable
    {
        private readonly ITaskStore _taskStore;

        public Module(ITaskStore taskStore)
        {
            _taskStore = taskStore;
        }

        public static string SystemName => "Smartstore.AmazonPay";

        public RouteInfo GetConfigurationRoute()
            => new("Configure", "AmazonPayAdmin", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<AmazonPaySettings>();
            await ImportLanguageResourcesAsync();

            _ = await _taskStore.GetOrAddTaskAsync<DataPollingTask>(x =>
            {
                x.Name = Services.Localization.GetResource("Plugins.Payments.AmazonPay.TaskName");
                x.CronExpression = "*/30 * * * *";  // 30 minutes.
            });

            await base.InstallAsync(context);
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<AmazonPaySettings>();
            await DeleteLanguageResourcesAsync();

            await _taskStore.TryDeleteTaskAsync<DataPollingTask>();

            await base.UninstallAsync();
        }
    }
}
