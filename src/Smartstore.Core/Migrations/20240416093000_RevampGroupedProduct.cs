using FluentMigrator;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2024-04-16 09:30:00", "Core: revamp grouped product")]
    internal class RevampGroupedProduct : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string ProductTableName = nameof(Product);
        const string ConfigurationColumn = "ProductTypeConfiguration";

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
                "Page size of associated products list",
                "Listengröße der verknüpften Produkte");

            builder.AddOrUpdate("Admin.Catalog.Products.GroupedProductConfiguration.HeaderFields",
                "Header fields",
                "Felder in der Titelzeile",
                "Specifies additional fields for the header of an associated product. The product name is always displayed.",
                "Legt zusätzliche Felder für die Titelzeile eines verknüpften Produktes fest. Produktname wird immer angezeigt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Media.AssociatedProductHeaderThumbSize",
                "Associated (grouped) product in the header",
                "Verknüpftes (Gruppen)-Produkt in der Titelzeile");

            builder.AddOrUpdate("Products.DimensionsValue.Short",
                "{0} × {1} × {2}",
                "{0} × {1} × {2}");
            builder.AddOrUpdate("Products.DimensionsValue.Full",
                "{0} × {1} × {2} {3} (W×H×L)",
                "{0} × {1} × {2} {3} (B×H×L)");
        }
    }
}