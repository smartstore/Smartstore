using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-08-28 16:00:00", "Core: Attribute combination compare price")]
    internal class AttributeCombinationComparePrice : Migration
    {
        const string AttributeCombinationTable = nameof(ProductVariantAttributeCombination);
        const string ComparePriceColumn = nameof(ProductVariantAttributeCombination.ComparePrice);

        public override void Up()
        {
            if (!Schema.Table(AttributeCombinationTable).Column(ComparePriceColumn).Exists())
            {
                Create.Column(ComparePriceColumn).OnTable(AttributeCombinationTable)
                    .AsDecimal(18, 4)
                    .Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(AttributeCombinationTable).Column(ComparePriceColumn).Exists())
            {
                Delete.Column(ComparePriceColumn).FromTable(AttributeCombinationTable);
            }
        }
    }
}
