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
            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountLimitationsInLists",
                "Validate discount limitations in product lists",
                "Prüfe die Rabattgültigkeit in Produktlisten",
                "Enabling this option may reduce the performance.",
                "Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountGiftCardsInLists",
                "Check cart for gift cards when validating discounts in product lists",
                "Prüfe den Warenkorb auf Gutscheine bei der Rabattvalidierung in Produktlisten",
                "Specifies whether to check the shopping cart for the existence of gift cards when validating discounts in product lists. In case of gift cards no discount is applied because the customer could earn money through that. Enabling this option may reduce the performance.",
                "Legt fest, ob bei der Rabattvalidierung in Produktlisten der Warenkorb auf vorhandene Gutscheine überprüft werden soll. Bei Gutscheinen wird kein Rabatt gewährt, weil der Kunde damit Geld verdienen könnte. Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountRulesInLists",
                "Validate cart rules of discounts in product lists",
                "Prüfe Warenkorbregeln von Rabatten in Produktlisten",
                "Enabling this option may reduce the performance.",
                "Die Aktivierung dieser Option kann die Performance beeinträchtigen.");
        }
    }
}