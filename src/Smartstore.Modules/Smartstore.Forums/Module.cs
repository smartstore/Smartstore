global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentValidation;
global using Newtonsoft.Json;
global using Smartstore.Core.Localization;
global using Smartstore.Forums.Domain;
global using Smartstore.Forums.Models;
global using Smartstore.Forums.Services;
global using Smartstore.Web.Modelling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine.Modularity;
using Smartstore.Forums.Migrations;
using Smartstore.Http;

namespace Smartstore.Forums
{
    internal class Module : ModuleBase, IConfigurable
    {
        public static string SystemName => "Smartstore.Forums";

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("List", "ForumAdmin", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<ForumSettings>();
            await TrySaveSettingsAsync<ForumSearchSettings>();
            await ImportLanguageResourcesAsync();
            await TrySeedData(context);

            await base.InstallAsync(context);
        }

        private async Task TrySeedData(ModuleInstallationContext context)
        {
            try
            {
                var seeder = new ForumInstallationDataSeeder(context);
                await seeder.SeedAsync(Services.DbContext);
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "ForumsSampleDataSeeder failed.");
            }
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<ForumSearchSettings>();
            await DeleteSettingsAsync<ForumSettings>();
            await DeleteLanguageResourcesAsync();

            await base.UninstallAsync();
        }
    }
}
