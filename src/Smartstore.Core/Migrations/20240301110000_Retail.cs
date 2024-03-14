using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2024-03-01 11:00:00", "Core: order billing address optional")]
    internal class Retail : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
            Alter.Column(nameof(Order.BillingAddressId)).OnTable(nameof(Order)).AsInt32().Nullable();
        }

        public override void Down()
        {
            Alter.Column(nameof(Order.BillingAddressId)).OnTable(nameof(Order)).AsInt32().NotNullable();
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
        }
    }
}
