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
        const string RoundCartRuleColumn = nameof(Currency.RoundCartRule);

        public override void Up()
        {
            if (!Schema.Table(CurrencyTable).Column(RoundCartRuleColumn).Exists())
            {
                Create.Column(RoundCartRuleColumn).OnTable(CurrencyTable).AsInt32().Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(CurrencyTable).Column(RoundCartRuleColumn).Exists())
            {
                Delete.Column(RoundCartRuleColumn).FromTable(CurrencyTable);
            }
        }

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);

#pragma warning disable 612, 618
            await context.Currencies
                .Where(x => x.RoundOrderItemsEnabled)
                .ExecuteUpdateAsync(x => x.SetProperty(c => c.RoundCartRule, p => CartRoundingRule.AlwaysRound), cancelToken);
#pragma warning restore 612, 618
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
        }
    }
}
