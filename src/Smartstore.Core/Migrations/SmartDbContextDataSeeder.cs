using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            //await MigrateSettingsAsync(context, cancelToken);
        }

        //public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        //{
        //    await context.SaveChangesAsync(cancelToken);
        //}

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.Themes.Option.AssetCachingEnabled.Hint",
                "Determines whether compiled asset files should be cached in file system in order to speed up application restarts. Select 'Auto' if caching should depend on the current environment (Debug = disabled, Production = enabled).",
                "Legt fest, ob kompilierte JS- und CSS-Dateien wie bspw. 'Sass' im Dateisystem zwischengespeichert werden sollen, um den Programmstart zu beschleunigen. Wählen Sie 'Automatisch', wenn das Caching von der aktuellen Umgebung abhängig sein soll (Debug = deaktiviert, Produktiv = aktiviert).");

            builder.AddOrUpdate("Admin.Configuration.Themes.Option.BundleOptimizationEnabled.Hint",
                "Determines whether asset files (JS and CSS) should be grouped together in order to speed up page rendering. Select 'Auto' if bundling should depend on the current environment (Debug = disabled, Production = enabled).",
                "Legt fest, ob JS- und CSS-Dateien in Gruppen zusammengefasst werden sollen, um den Seitenaufbau zu beschleunigen. Wählen Sie 'Automatisch', wenn das Bundling von der aktuellen Umgebung abhängig sein soll (Debug = deaktiviert, Produktiv = aktiviert).");

            builder.AddOrUpdate("Admin.System.Warnings.TaskScheduler.Fail",
                "The task scheduler cannot poll and execute tasks. Base URL: {0}, Status: {1}. Please specify a working base url in appsettings.json, setting 'Smartstore.TaskSchedulerBaseUrl'.",
                "Der Task-Scheduler kann keine Hintergrund-Aufgaben planen und ausführen. Basis-URL: {0}, Status: {1}. Bitte legen Sie eine vom Webserver erreichbare Basis-URL in der Datei appsettings.json Datei fest, Einstellung: 'Smartstore.TaskSchedulerBaseUrl'.");

            // Admin.Common.DataSuccessfullySaved
            builder.AddOrUpdate("Admin.Common.DataSuccessfullySaved",
                "The data was saved successfully",
                "Die Daten wurden erfolgreich gespeichert");

        }
    }
}