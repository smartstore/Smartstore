using FluentMigrator;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2024-04-16 09:30:00", "Core: Grouped product configuration")]
    internal class GroupedProductConfiguration : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string ProductTableName = nameof(Product);
        const string ConfigurationColumn = nameof(Product.ProductTypeConfiguration);

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public override void Up()
        {
            if (!Schema.Table(ProductTableName).Column(ConfigurationColumn).Exists())
            {
                Create.Column(ConfigurationColumn).OnTable(ProductTableName).AsString(int.MaxValue).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(ProductTableName).Column(ConfigurationColumn).Exists())
            {
                Delete.Column(ConfigurationColumn).FromTable(ProductTableName);
            }
        }

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Catalog.Products.GroupedProductConfiguration.PageSize",
                "Page size of associated products",
                "Listengröße der verknüpften Produkte");

            builder.AddOrUpdate("Admin.Catalog.Products.GroupedProductConfiguration.Collapsable",
                "Expand/collapse associated products",
                "Verknüpfte Produkte auf-/zuklappen");

            builder.AddOrUpdate("Admin.Catalog.Products.GroupedProductConfiguration.HeaderFields",
                "Header fields",
                "Felder für die Kopfzeile",
                "Specifies additional fields for the header of an associated product. The product name and SKU are always displayed.",
                "Legt zusätzliche Felder für die Kopfzeile eines verknüpften Produktes fest. Produktname und -nummer (SKU) werden immer angezeigt.");
        }
    }
}