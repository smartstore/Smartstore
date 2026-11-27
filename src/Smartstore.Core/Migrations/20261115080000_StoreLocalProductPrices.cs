using System.Data;
using FluentMigrator;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Common;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-11-15 08:00:00", "Core: Store-local product prices")]
internal class StoreLocalProductPrices : Migration
{
    private const string TableName = nameof(ProductStorePrice);
    private const string UniqueIndex = "UX_ProductStorePrice_Product_Store";

    public override void Up()
    {
        if (!Schema.Table(TableName).Exists())
        {
            Create.Table(TableName)
                .WithIdColumn()
                .WithColumn(nameof(ProductStorePrice.ProductId)).AsInt32().NotNullable()
                    .ForeignKey("Product", nameof(BaseEntity.Id)).OnDelete(Rule.Cascade)
                .WithColumn(nameof(ProductStorePrice.StoreId)).AsInt32().NotNullable()
                    .ForeignKey("Store", nameof(BaseEntity.Id)).OnDelete(Rule.Cascade)
                .WithColumn(nameof(ProductStorePrice.CurrencyId)).AsInt32().NotNullable()
                    .ForeignKey("Currency", nameof(BaseEntity.Id)).OnDelete(Rule.None)
                .WithColumn(nameof(ProductStorePrice.Price)).AsDecimal(18, 4).NotNullable();
        }

        if (!Schema.Table(TableName).Index(UniqueIndex).Exists())
        {
            Create.Index(UniqueIndex)
                .OnTable(TableName)
                .OnColumn(nameof(ProductStorePrice.ProductId)).Ascending()
                .OnColumn(nameof(ProductStorePrice.StoreId)).Ascending()
                .WithOptions().Unique();
        }
    }

    public override void Down()
    {
        // Merchant-entered prices must not be silently deleted during rollback.
    }
}
