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
            builder.AddOrUpdate("Payment.MissingConfirmationUrl",
                "The payment could not be confirmed. The payment method you selected <strong>{0}</strong> did not provide the redirect URL required for confirmation.",
                "Die Zahlung konnte nicht bestätigt werden. Die von Ihnen gewählte Zahlungsmethode <strong>{0}</strong> hat keine für die Bestätigung erforderliche Weiterleitungs-URL geliefert.");
        }
    }
}