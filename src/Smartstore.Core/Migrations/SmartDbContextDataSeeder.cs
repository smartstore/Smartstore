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
            builder.AddOrUpdate("Products.Price.OfferCountdown",
                "Ends in <b class=\"fwm\">{0}</b>",
                "Endet in <b class=\"fwm\">{0}</b>");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerNameFormat.Hint",
                "Sets the customer's display name to be used for public content such as product reviews, comments, etc..",
                "Legt den Anzeigenamen des Kunden fest, der für öffentliche Inhalte wie Produktbewertungen, Kommentare, etc. verwendet wird.");
        }
    }
}