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
        const string MidpointRoundingColumn = nameof(Currency.MidpointRounding);
        const string RoundOrderItemsEnabledColumn = nameof(Currency.RoundOrderItemsEnabled);
        const string RoundNetPricesColumn = nameof(Currency.RoundNetPrices);
        const string RoundUnitPricesColumn = nameof(Currency.RoundUnitPrices);

        public override void Up()
        {
            // Make nullable.
            Alter.Column(RoundOrderItemsEnabledColumn).OnTable(CurrencyTable).AsBoolean().Nullable();

            if (!Schema.Table(CurrencyTable).Column(MidpointRoundingColumn).Exists())
            {
                Create.Column(MidpointRoundingColumn).OnTable(CurrencyTable).AsInt32().NotNullable().WithDefaultValue((int)MidpointRounding.ToEven);
            }

            if (!Schema.Table(CurrencyTable).Column(RoundNetPricesColumn).Exists())
            {
                Create.Column(RoundNetPricesColumn).OnTable(CurrencyTable).AsBoolean().Nullable();
            }

            if (!Schema.Table(CurrencyTable).Column(RoundUnitPricesColumn).Exists())
            {
                Create.Column(RoundUnitPricesColumn).OnTable(CurrencyTable).AsBoolean().Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(CurrencyTable).Column(MidpointRoundingColumn).Exists())
            {
                Delete.Column(MidpointRoundingColumn).FromTable(CurrencyTable);
            }

            if (Schema.Table(CurrencyTable).Column(RoundNetPricesColumn).Exists())
            {
                Delete.Column(RoundNetPricesColumn).FromTable(CurrencyTable);
            }

            if (Schema.Table(CurrencyTable).Column(RoundUnitPricesColumn).Exists())
            {
                Delete.Column(RoundUnitPricesColumn).FromTable(CurrencyTable);
            }
        }

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.Currencies
                .Where(x => x.RoundOrderItemsEnabled != null && x.RoundOrderItemsEnabled.Value == true)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(c => c.RoundNetPrices, p => true)
                    .SetProperty(c => c.RoundUnitPrices, p => true),
                    cancelToken);

            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
        }
    }
}
