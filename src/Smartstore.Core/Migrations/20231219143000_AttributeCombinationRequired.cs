using FluentMigrator;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-12-19 14:30:00", "Core: attribute combination required")]
    internal class AttributeCombinationRequired : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string ProductTable = nameof(Product);
        const string CombinationRequiredColumn = nameof(Product.AttributeCombinationRequired);

        public override void Up()
        {
            if (!Schema.Table(ProductTable).Column(CombinationRequiredColumn).Exists())
            {
                Create.Column(CombinationRequiredColumn).OnTable(ProductTable).AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table(ProductTable).Column(CombinationRequiredColumn).Exists())
            {
                Delete.Column(CombinationRequiredColumn).FromTable(ProductTable);
            }
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Catalog.Products.Fields.AttributeCombinationRequired",
                "Attribute combination is required",
                "Attribut-Kombination ist erforderlich",
                "Specifies whether an attribute combination must exist for the attributes selected in the frontend, otherwise the product cannot be ordered.",
                "Legt fest, ob für die im Frontend gewählten Attribute eine Attributkombination existieren muss, damit das Produkt bestellt werden kann.");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.Description",
                "Attribute combinations allow different product characteristics on the basis of specific combinations.",
                "Attribut-Kombinationen ermöglichen die Erfassung abweichender Produkt-Eigenschaften auf Basis von spezifischen Kombinationen.");
        }
    }
}
