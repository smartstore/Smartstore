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

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Swatches",
            "Color and Image Swatches",
            "Farb- und Bildmuster");

        builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchSizeId",
            "Swatch Size",
            "Mustergröße",
            "Specifies the size of color and image swatches for this attribute.",
            "Legt die Größe von Farb- und Bildmustern für dieses Attribut fest.");
    }
}
