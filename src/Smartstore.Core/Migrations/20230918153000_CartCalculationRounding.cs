using FluentMigrator;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-09-18 15:30:00", "Core: shopping cart calculation rounding")]
    internal class CartCalculationRounding : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string CurrencyTable = nameof(Currency);
        const string RoundOrderItemsEnabledColumn = nameof(Currency.RoundOrderItemsEnabled);
        const string RoundForNetPricesColumn = nameof(Currency.RoundForNetPrices);

        public override void Up()
        {
            // Make nullable.
            Alter.Column(RoundOrderItemsEnabledColumn).OnTable(CurrencyTable).AsBoolean().Nullable();

            if (!Schema.Table(CurrencyTable).Column(RoundForNetPricesColumn).Exists())
            {
                Create.Column(RoundForNetPricesColumn).OnTable(CurrencyTable).AsBoolean().Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(CurrencyTable).Column(RoundForNetPricesColumn).Exists())
            {
                Delete.Column(RoundForNetPricesColumn).FromTable(CurrencyTable);
            }
        }

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);

            await context.Currencies
                .Where(x => x.RoundOrderItemsEnabled != null && x.RoundOrderItemsEnabled.Value == true)
                .ExecuteUpdateAsync(x => x.SetProperty(c => c.RoundForNetPrices, p => true), cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
        }
    }
}
