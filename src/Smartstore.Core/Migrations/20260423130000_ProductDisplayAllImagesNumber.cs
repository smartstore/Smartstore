using FluentMigrator;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-04-23 13:00:00", "Core: Product display all images number")]
internal class ProductDisplayAllImagesNumber : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string ProductTable = nameof(Product);
    const string DisplayAllImagesNumberColumn = nameof(Product.DisplayAllImagesNumber);

    public override void Up()
    {
        if (!Schema.Table(ProductTable).Column(DisplayAllImagesNumberColumn).Exists())
        {
            Create.Column(DisplayAllImagesNumberColumn).OnTable(ProductTable)
                .AsInt32()
                .Nullable()
                .Indexed();
        }
    }

    public override void Down()
    {
        if (Schema.Table(ProductTable).Column(DisplayAllImagesNumberColumn).Exists())
        {
            Delete.Column(DisplayAllImagesNumberColumn).FromTable(ProductTable);
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
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DisplayAllImagesNumber",
            "Only show selected variant images from this number of images",
            "Ab dieser Bildanzahl nur Bilder der gewählten Variante anzeigen",
            "If the number of images reaches this value, only the images of the selected variant are shown. If fewer images are available, all images are shown.",
            "Sobald diese Bildanzahl erreicht ist, werden nur noch die Bilder der gewählten Variante angezeigt. Bei weniger Bildern werden weiterhin alle Bilder angezeigt.");
    }
}