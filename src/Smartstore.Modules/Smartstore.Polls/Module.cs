using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Polls.Components;
using Smartstore.Polls.Migrations;

namespace Smartstore.Polls
{
    internal class Module : ModuleBase, IConfigurable, IWidget
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        // TODO: (mh) (core) Lead to polls configuration
        public RouteInfo GetConfigurationRoute()
            => new("Settings", "News", new { area = "Admin" });

        public WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId)
        {
            return new ComponentWidgetInvoker(typeof(HomepagePollsViewComponent), null);
        }

        public string[] GetWidgetZones()
        {
            return new string[] { "home_page_after_tags" };
        }

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResources();
            await TrySeedData(context);

            await base.InstallAsync(context);
        }

        private async Task TrySeedData(ModuleInstallationContext context)
        {
            try
            {
                var seeder = new PollsInstallationDataSeeder(Services.Resolve<SmartDbContext>(), context, Services.Resolve<IWidgetService>());
                await seeder.SeedAsync(Services.DbContext);
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "PollsSampleDataSeeder failed.");
            }
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResources();

            await base.UninstallAsync();
        }
    }
}
