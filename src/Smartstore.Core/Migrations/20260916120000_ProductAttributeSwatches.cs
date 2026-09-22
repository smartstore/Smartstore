using FluentMigrator;
using FluentMigrator.Builders.Create.Column;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-09-16 12:00:00", "Core: Product attribute swatches")]
internal class ProductAttributeSwatches : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string AttrTableName = "ProductAttribute";
    const string ProductAttrTableName = "Product_ProductAttribute_Mapping";

    public override void Up()
    {
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchSize), c => c.AsInt32().Nullable());
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchAspectRatio), c => c.AsDecimal(18, 4).NotNullable().WithDefaultValue(1m));
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchShape), c => c.AsInt32().Nullable());
        Migrate(AttrTableName, nameof(ProductAttribute.ShowValueNameInSwatch), c => c.AsBoolean().NotNullable().WithDefaultValue(false));
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchPriceDisplay), c => c.AsInt32().NotNullable().WithDefaultValue(0));

        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchSize), c => c.AsInt32().Nullable());
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchAspectRatio), c => c.AsDecimal(18, 4).Nullable());
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchShape), c => c.AsInt32().Nullable());
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.ShowValueNameInSwatch), c => c.AsBoolean().Nullable());
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchPriceDisplay), c => c.AsInt32().Nullable());
    }

    public override void Down()
    {
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchSize), null);
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchAspectRatio), null);
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchShape), null);
        Migrate(AttrTableName, nameof(ProductAttribute.ShowValueNameInSwatch), null);
        Migrate(AttrTableName, nameof(ProductAttribute.SwatchPriceDisplay), null);

        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchSize), null);
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchAspectRatio), null);
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchShape), null);
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.ShowValueNameInSwatch), null);
        Migrate(ProductAttrTableName, nameof(ProductVariantAttribute.SwatchPriceDisplay), null);
    }

    private void Migrate(string table, string column, Action<ICreateColumnAsTypeSyntax> configure)
    {
        var exists = Schema.Table(table).Column(column).Exists();

        if (!exists && configure != null)
        {
            var schema = Create.Column(column).OnTable(table);
            configure(schema);
        }
        else if (exists && configure == null)
        {
            Delete.Column(column).FromTable(table);
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
        builder.AddOrUpdate("Enums.SwatchSize.XSmall", "XS", "XS");
        builder.AddOrUpdate("Enums.SwatchSize.Small", "S", "S");
        builder.AddOrUpdate("Enums.SwatchSize.Medium", "M", "M");
        builder.AddOrUpdate("Enums.SwatchSize.Large", "L", "L");
        builder.AddOrUpdate("Enums.SwatchSize.XLarge", "XL", "XL");
        builder.AddOrUpdate("Enums.SwatchSize.XXLarge", "XXL", "XXL");

        builder.AddOrUpdate("Enums.SwatchShape.Rounded", "Pill", "Abgerundet");
        builder.AddOrUpdate("Enums.SwatchShape.Rect", "Rectangular", "Rechteckig");
        builder.AddOrUpdate("Enums.SwatchShape.Circle", "Circle", "Rund");

        builder.AddOrUpdate("Enums.SwatchPriceDisplayMode.None", "None", "Keine");
        builder.AddOrUpdate("Enums.SwatchPriceDisplayMode.Adjustment",
            "Price adjustment or product price", 
            "Preisanpassung oder Produktpreis");
        builder.AddOrUpdate("Enums.SwatchPriceDisplayMode.FinalPrice",
            "Product, Comparison, and Base Price",
            "Produkt-, Vergleichs- und Grundpreis");

        builder.AddOrUpdate("Admin.Common.ResetToDefault",
            "Reset to default value.",
            "Auf den Standardwert zurücksetzen.");

        builder.AddOrUpdate("Admin.Common.ClickToSetDifferentValue",
            "Set a value that differs from the default by clicking.",
            "Klicken, um einen vom Standard abweichenden Wert festzulegen.");

        builder.AddOrUpdate("Admin.Common.ClickToSetValue",
            "No value set (default). Click to set a value.",
            "Kein Wert gesetzt (Standard). Klicken, um einen Wert festzulegen.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Swatches",
            "Color and Image Swatches",
            "Farb- und Bildmuster");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.EditSwatches",
            "Edit color and image swatches",
            "Farb- und Bildmuster bearbeiten");


        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchSize",
            "Default size of swatches",
            "Standardgröße von Farb- und Bildmustern",
            "Specifies the default size for product attribute swatches. This setting can be overridden at both the attribute and product levels. The recommended size is M.",
            "Legt die Standardgröße von Farb- und Bildmustern bei Produktattributen fest. Diese Einstellung kann sowohl beim Attribut als auch beim Produkt überschrieben werden."
             + " Die empfohlene Größe ist M.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchShape",
            "Default shape of swatches",
            "Standardform von Farb- und Bildmustern",
            "Specifies the default shape for product attribute swatches. This setting can be overridden at both the attribute and product levels. Default is \"Pill\".",
            "Legt die Standardform von Farb- und Bildmustern bei Produktattributen fest. Diese Einstellung kann sowohl beim Attribut als auch beim Produkt überschrieben werden."
            + " Standard ist \"Abgerundet\".");


        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchSize",
            "Swatch Size",
            "Mustergröße",
            "Specifies the size of color and image swatches.",
            "Legt die Größe von Farb- und Bildmustern fest.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchAspectRatio",
            "Swatch Aspect Ratio",
            "Muster-Seitenverhältnis",
            "Specifies the height-to-width ratio of color and image swatches. Default is 1:1 (square).",
            "Legt das Höhen-Breiten-Verhältnis von Farb- und Bildmustern fest. Standard ist 1:1 (quadratisch).");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchShape",
            "Swatch Shape",
            "Musterform",
            "Specifies the shape of color and image swatches.",
            "Legt die Form von Farb- und Bildmustern fest.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.ShowValueNameInSwatch",
            "Show option name",
            "Optionsnamen anzeigen",
            "Specifies whether to display the option name in color and image swatches.",
            "Legt fest, ob der Optionsname in Farb- und Bildmustern angezeigt wird.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchPriceDisplay",
            "Swatch Price Display",
            "Preisanzeige im Muster",
            "Specifies which price information is shown in color and image swatches.",
            "Legt fest, welche Preisinformationen in Farb- und Bildmustern angezeigt werden.");


        builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.SwatchInfo",
            "These settings for swatches allow you to specify values for this product that differ from the attribute.",
            "Diese Einstellungen für Farb- und Bildmuster ermöglichen es, zum Attribut abweichende Werte für dieses Produkt festzulegen.");
    }
}
