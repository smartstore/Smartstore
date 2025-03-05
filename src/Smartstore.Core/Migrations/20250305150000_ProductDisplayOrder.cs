using FluentMigrator;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-03-05 15:00:00", "Core: product display order")]
    internal class ProductDisplayOrder : Migration
    {
        const string ProductTableName = nameof(Product);
        const string DisplayOrderColumn = nameof(Product.DisplayOrder);
        const string DisplayOrderIndex = "IX_Product_DisplayOrder";

        public override void Up()
        {
            if (!Schema.Table(ProductTableName).Index(DisplayOrderColumn).Exists())
            {
                Create.Index(DisplayOrderIndex)
                    .OnTable(ProductTableName)
                    .OnColumn(DisplayOrderColumn)
                    .Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {
            if (Schema.Table(ProductTableName).Index(DisplayOrderIndex).Exists())
            {
                Delete.Index(DisplayOrderIndex).OnTable(ProductTableName);
            }
        }
    }
}
