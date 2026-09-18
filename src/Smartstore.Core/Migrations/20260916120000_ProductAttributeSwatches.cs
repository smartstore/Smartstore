using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-09-16 12:00:00", "Core: Product attribute swatches")]
internal class ProductAttributeSwatches : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string TableName = nameof(ProductAttribute);
    const string SwatchSizeIdColumn = nameof(ProductAttribute.SwatchSizeId);
    const string SwatchShapeIdColumn = nameof(ProductAttribute.SwatchShapeId);
    const string SwatchAspectRatioColumn = nameof(ProductAttribute.SwatchAspectRatio);
    const string ShowValueNameInSwatchColumn = nameof(ProductAttribute.ShowValueNameInSwatch);
    const string SwatchPriceDisplayIdColumn = nameof(ProductAttribute.SwatchPriceDisplayId);

    public override void Up()
    {
        if (!Schema.Table(TableName).Column(SwatchSizeIdColumn).Exists())
        {
            Create.Column(SwatchSizeIdColumn).OnTable(TableName)
                .AsInt32()
                .Nullable();
        }

        if (!Schema.Table(TableName).Column(SwatchShapeIdColumn).Exists())
        {
            Create.Column(SwatchShapeIdColumn).OnTable(TableName)
                .AsInt32()
                .Nullable();
        }

        if (!Schema.Table(TableName).Column(SwatchAspectRatioColumn).Exists())
        {
            Create.Column(SwatchAspectRatioColumn).OnTable(TableName)
                .AsDecimal(18, 4)
                .NotNullable()
                .WithDefaultValue(1m);
        }

        if (!Schema.Table(TableName).Column(ShowValueNameInSwatchColumn).Exists())
        {
            Create.Column(ShowValueNameInSwatchColumn).OnTable(TableName)
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);
        }

        if (!Schema.Table(TableName).Column(SwatchPriceDisplayIdColumn).Exists())
        {
            Create.Column(SwatchPriceDisplayIdColumn).OnTable(TableName)
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);
        }
    }

    public override void Down()
    {
        if (Schema.Table(TableName).Column(SwatchPriceDisplayIdColumn).Exists())
        {
            Delete.Column(SwatchPriceDisplayIdColumn).FromTable(TableName);
        }

        if (Schema.Table(TableName).Column(ShowValueNameInSwatchColumn).Exists())
        {
            Delete.Column(ShowValueNameInSwatchColumn).FromTable(TableName);
        }

        if (Schema.Table(TableName).Column(SwatchAspectRatioColumn).Exists())
        {
            Delete.Column(SwatchAspectRatioColumn).FromTable(TableName);
        }

        if (Schema.Table(TableName).Column(SwatchShapeIdColumn).Exists())
        {
            Delete.Column(SwatchShapeIdColumn).FromTable(TableName);
        }

        if (Schema.Table(TableName).Column(SwatchSizeIdColumn).Exists())
        {
            Delete.Column(SwatchSizeIdColumn).FromTable(TableName);
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

        builder.AddOrUpdate("Enums.SwatchShape.Rounded", "Rounded corners", "Ecken abgerundet");
        builder.AddOrUpdate("Enums.SwatchShape.Rect", "Sharp corners", "Eckig");
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

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchSize",
            "Swatch Size",
            "Mustergröße",
            "Specifies the size of color and image swatches.",
            "Legt die Größe von Farb- und Bildmustern fest.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchAspectRatio",
            "Swatch Aspect Ratio",
            "Muster-Seitenverhältnis",
            "Specifies the height-to-width ratio of color and image swatches. Default is 1:1.",
            "Legt das Höhen-Breiten-Verhältnis von Farb- und Bildmustern fest. Standard ist 1:1.");

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

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchSize",
            "Default size of swatches",
            "Standardgröße von Farb- und Bildmustern",
            "Specifies the default size for product attribute swatches. This setting can be overridden at both the attribute and product levels. Default is M.",
            "Legt die Standardgröße von Farb- und Bildmustern bei Produktattributen fest. Diese Einstellung kann sowohl beim Attribut als auch beim Produkt überschrieben werden."
             + " Der Standardwert ist M.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchShape",
            "Default shape of swatches",
            "Standardform von Farb- und Bildmustern",
            "Specifies the default shape for product attribute swatches. This setting can be overridden at both the attribute and product levels. Default is \"Rounded corners\".",
            "Legt die Standardform von Farb- und Bildmustern bei Produktattributen fest. Diese Einstellung kann sowohl beim Attribut als auch beim Produkt überschrieben werden."
            + " Standard ist \"Ecken abgerundet\".");
    }
}
