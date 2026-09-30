using FluentMigrator;
using FluentMigrator.Builders.Create.Column;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-09-16 12:00:00", "Core: Product attribute swatches")]
internal class ProductAttributeSwatches : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string AttrTableName = "ProductAttribute";
    const string ProductAttrTableName = "Product_ProductAttribute_Mapping";
    const string ProductVariantValueTableName = "ProductVariantAttributeValue";
    const string ProductAttributeOptionTableName = "ProductAttributeOption";
    const string CheckoutAttributeTableName = "CheckoutAttribute";
    const string CheckoutAttributeValueTableName = "CheckoutAttributeValue";

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

        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchSize), c => c.AsInt32().Nullable());
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchAspectRatio), c => c.AsDecimal(18, 4).NotNullable().WithDefaultValue(1m));
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchShape), c => c.AsInt32().Nullable());
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.ShowValueNameInSwatch), c => c.AsBoolean().NotNullable().WithDefaultValue(false));
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchPriceDisplay), c => c.AsInt32().NotNullable().WithDefaultValue(0));

        Migrate(ProductVariantValueTableName, nameof(ProductVariantAttributeValue.AdditionalColors), c => c.AsString(100).Nullable());
        Migrate(ProductAttributeOptionTableName, nameof(ProductAttributeOption.AdditionalColors), c => c.AsString(100).Nullable());
        Migrate(CheckoutAttributeValueTableName, nameof(CheckoutAttributeValue.AdditionalColors), c => c.AsString(100).Nullable());
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

        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchSize), null);
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchAspectRatio), null);
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchShape), null);
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.ShowValueNameInSwatch), null);
        Migrate(CheckoutAttributeTableName, nameof(CheckoutAttribute.SwatchPriceDisplay), null);

        Migrate(ProductVariantValueTableName, nameof(ProductVariantAttributeValue.AdditionalColors), null);
        Migrate(ProductAttributeOptionTableName, nameof(ProductAttributeOption.AdditionalColors), null);
        Migrate(CheckoutAttributeValueTableName, nameof(CheckoutAttributeValue.AdditionalColors), null);
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
        builder.AddOrUpdate("Enums.SwatchPriceDisplayMode.Adjustment", "Price adjustment", "Preisanpassung");
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

        builder.AddOrUpdate("Enums.AttributeControlType.Boxes",
            "Color and Image Swatches",
            "Farb- und Bildmuster");
        builder.AddOrUpdate("Enums.FacetTemplateHint.Custom",
            "Color and Image Swatches",
            "Farb- und Bildmuster");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Swatches",
            "Color and Image Swatches",
            "Farb- und Bildmuster");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.EditSwatches",
            "Edit color and image swatches",
            "Farb- und Bildmuster bearbeiten");


        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchSize",
            "Default size of swatches",
            "Standardgröße von Farb- und Bildmustern",
            "Sets the default swatch size. It can be overridden for product and checkout attributes and for individual products. Recommended size is M.",
            "Legt die Standardgröße der Muster fest. Sie kann bei Produkt- und Checkoutattributen sowie bei einzelnen Produkten überschrieben werden. Empfohlen wird Größe M.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultSwatchShape",
            "Default shape of swatches",
            "Standardform von Farb- und Bildmustern",
            "Sets the default swatch shape. It can be overridden for product and checkout attributes and for individual products. Default is \"Pill\".",
            "Legt die Standardform der Muster fest. Sie kann bei Produkt- und Checkoutattributen sowie bei einzelnen Produkten überschrieben werden. Standard ist \"Abgerundet\".");


        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchSize",
            "Swatch Size",
            "Mustergröße",
            "Sets the swatch size. Names and prices appear inside the swatch from size L when every option has a color or image.",
            "Legt die Mustergröße fest. Namen und Preise erscheinen ab Größe L im Muster, wenn jede Option eine Farbe oder ein Bild besitzt.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchAspectRatio",
            "Swatch Aspect Ratio",
            "Muster-Seitenverhältnis",
            "Sets the height-to-width ratio. Default is 1:1 (square). It only affects card-style swatches, used from size L when name or price display is enabled and every option has a color or image.",
            "Legt das Höhen-Breiten-Verhältnis fest. Standard ist 1:1 (quadratisch). Es wirkt nur im Kartenformat, das ab Größe L bei aktivierter Namens- oder Preisanzeige verwendet wird, wenn jede Option eine Farbe oder ein Bild besitzt.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchShape",
            "Swatch Shape",
            "Musterform",
            "Sets the swatch shape. Circle is replaced by Pill when names or prices are shown inside the swatch or an option has no color or image.",
            "Legt die Musterform fest. Rund wird durch Abgerundet ersetzt, wenn Namen oder Preise im Muster erscheinen oder eine Option keine Farbe und kein Bild besitzt.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.ShowValueNameInSwatch",
            "Show option name",
            "Optionsnamen anzeigen",
            "Shows the option name inside the swatch. The name is always shown with price information. Card layout requires size L or larger and a color or image for every option.",
            "Zeigt den Optionsnamen im Muster an. Bei eingeblendeten Preisinformationen wird der Name immer angezeigt. Das Kartenformat erfordert mindestens Größe L sowie eine Farbe oder ein Bild für jede Option.");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchPriceDisplay",
            "Swatch Price Display",
            "Preisanzeige im Muster",
            "Shows available price information inside the swatch. This requires visible prices, size L or larger, and a color or image for every option.",
            "Zeigt verfügbare Preisinformationen im Muster an. Dafür müssen Preise sichtbar sein, mindestens Größe L eingestellt sein und jede Option eine Farbe oder ein Bild besitzen.");


        builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.SwatchInfo",
            "Use these settings to override the default attribute values for this product.",
            "Legen Sie hier abweichende Einstellungen fest, wenn Sie die Vorgaben des Attributs für dieses Produkt überschreiben möchten.");

        builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.SwatchOverridesInfo",
            "Overwritten: {0}.",
            "Abweichend: {0}.");

        builder.AddOrUpdate("Admin.Common.ColorPalette.TooManyColors",
            "A maximum of {0} colors can be specified.",
            "Es können maximal {0} Farben angegeben werden.");

        builder.AddOrUpdate("Admin.Common.ColorPalette.PrimaryColorRequired",
            "Select the primary color before adding more colors.",
            "Wählen Sie zuerst die Hauptfarbe aus, bevor Sie weitere Farben hinzufügen.");
    }
}
