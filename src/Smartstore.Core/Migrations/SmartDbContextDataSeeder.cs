using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            return Task.CompletedTask;
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("AriaLabel.MainNavigation", "Main navigation", "Hauptnavigation");
            builder.AddOrUpdate("AriaLabel.SearchBox",
                "Type in a search term and press Enter to search for products.",
                "Geben Sie einen Suchbegriff ein und drücken Sie die Eingabetaste, um nach Produkten zu suchen.");

            builder.AddOrUpdate("Search.SearchBox.Clear", "Clear search term", "Suchbegriff löschen");
            builder.AddOrUpdate("Common.ScrollUp", "Scroll up", "Nach oben scrollen");
        }
    }
}