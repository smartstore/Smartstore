using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.News.Migrations;
using Smartstore.Core;
using Smartstore.Core.Messaging;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Core.Widgets;
using Smartstore.Web.Components;

namespace Smartstore.News
{
    internal class Module : ModuleBase, IConfigurable, IWidget
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("Settings", "News", new { area = "Admin" });

        public WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId)
        {
            return new ComponentWidgetInvoker(typeof(HomepageNewsViewComponent), null);
        }

        public string[] GetWidgetZones()
        {
            return new string[] { "home_page_after_tags" };
        }

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<NewsSettings>();
            await ImportLanguageResources();
            await TrySeedData(context);

            await base.InstallAsync(context);
        }

        private async Task TrySeedData(ModuleInstallationContext context)
        {
            try
            {
                var seeder = new NewsInstallationDataSeeder(context, Services.Resolve<IMessageTemplateService>());
                await seeder.SeedAsync(Services.DbContext);
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "NewsSampleDataSeeder failed.");
            }
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<NewsSettings>();
            await DeleteLanguageResources();

            await base.UninstallAsync();
        }
    }
}
