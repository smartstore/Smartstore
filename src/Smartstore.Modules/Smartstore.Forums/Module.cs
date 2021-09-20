using System;
using System.Threading.Tasks;
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
            => new("Configure", "Forum", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<ForumSettings>();
            await TrySaveSettingsAsync<ForumSearchSettings>();
            await ImportLanguageResources();
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
            await DeleteLanguageResources();

            await base.UninstallAsync();
        }
    }
}
