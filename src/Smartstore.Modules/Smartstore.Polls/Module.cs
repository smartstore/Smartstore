global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Newtonsoft.Json;
global using Smartstore.Core.Localization;
global using Smartstore.Polls.Domain;
global using Smartstore.Polls.Models;
global using Smartstore.Web.Modelling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
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

        public RouteInfo GetConfigurationRoute()
            => new("List", "PollAdmin", new { area = "Admin" });

        public WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId)
            => new ComponentWidgetInvoker(typeof(HomepagePollsViewComponent), null);

        public string[] GetWidgetZones()
            => new string[] { "home_page_after_tags" };

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await ImportLanguageResourcesAsync();
            await TrySeedData(context);

            await base.InstallAsync(context);
        }

        private async Task TrySeedData(ModuleInstallationContext context)
        {
            try
            {
                var seeder = new PollsInstallationDataSeeder(context, Services.Resolve<IWidgetService>());
                await seeder.SeedAsync(Services.DbContext);
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "PollsSampleDataSeeder failed.");
            }
        }

        public override async Task UninstallAsync()
        {
            await DeleteLanguageResourcesAsync();

            await base.UninstallAsync();
        }
    }
}
