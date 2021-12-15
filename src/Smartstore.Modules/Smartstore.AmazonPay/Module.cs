global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Smartstore.AmazonPay.Models;
global using Smartstore.AmazonPay.Providers;
global using Smartstore.Core.Localization;
global using Smartstore.Web.Modelling;
using System.Linq;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Scheduling;

namespace Smartstore.AmazonPay
{
    internal class Module : ModuleBase, ICookiePublisher
    {
        private readonly IProviderManager _providerManager;
        private readonly ITaskStore _taskStore;
        private readonly WidgetSettings _widgetSettings;

        public Module(
            IProviderManager providerManager,
            ITaskStore taskStore,
            WidgetSettings widgetSettings)
        {
            _providerManager = providerManager;
            _taskStore = taskStore;
            _widgetSettings = widgetSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public Task<IEnumerable<CookieInfo>> GetCookieInfoAsync()
        {
            var widget = _providerManager.GetProvider<IWidget>("Smartstore.AmazonPay");
            if (!widget.IsWidgetActive(_widgetSettings))
            {
                return null;
            }

            var cookieInfo = new CookieInfo
            {
                Name = T("Plugins.FriendlyName.Widgets.AmazonPay"),
                Description = T("Plugins.Payments.AmazonPay.CookieInfo"),
                CookieType = CookieType.Required
            };

            return Task.FromResult(new List<CookieInfo> { cookieInfo }.AsEnumerable());
        }

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
