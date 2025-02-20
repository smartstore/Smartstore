using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2025-02-20 10:30:00", "Core: Attribute combination weight")]
    internal class AttributeCombinationWeight : Migration
    {
        const string AttributeCombinationTable = nameof(ProductVariantAttributeCombination);
        const string WeightColumn = nameof(ProductVariantAttributeCombination.Weight);

        public override void Up()
        {
            if (!Schema.Table(AttributeCombinationTable).Column(WeightColumn).Exists())
            {
                Create.Column(WeightColumn).OnTable(AttributeCombinationTable)
                    .AsDecimal(18, 4)
                    .Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(AttributeCombinationTable).Column(WeightColumn).Exists())
            {
                Delete.Column(WeightColumn).FromTable(AttributeCombinationTable);
            }
        }
    }
}
