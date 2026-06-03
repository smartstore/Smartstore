using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations;

public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
{
    public DataSeederStage Stage => DataSeederStage.Early;
    public bool AbortOnFailure => false;

    public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        await MigrateSettingsAsync(context, cancelToken);
    }

    public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
    }

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        builder.Delete(
            "Admin.Orders.Products.AddNew.UnitPriceInclTax.Hint",
            "Admin.Orders.Products.AddNew.UnitPriceExclTax.Hint",
            "Admin.Orders.Products.AddNew.SubTotalInclTax.Hint",
            "Admin.Orders.Products.AddNew.SubTotalExclTax.Hint",
            "Admin.Orders.Products.Edit",
            "Admin.Orders.Products.Edit.InclTax",
            "Admin.Orders.Products.Edit.ExclTax");

        builder.AddOrUpdate("Admin.Orders.Products.AddNew.UnitPriceInclTax", "Unit price (gross)", "Einzelpreis (brutto)");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.UnitPriceExclTax", "Unit price (net)", "Einzelpreis (netto)");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.SubTotalInclTax", "Total (gross)", "Gesamt (brutto)");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.SubTotalExclTax", "Total (net)", "Gesamt (netto)");

        builder.AddOrUpdate("Admin.Orders.Fields.Edit.InclTax", "gross", "brutto");
        builder.AddOrUpdate("Admin.Orders.Fields.Edit.ExclTax", "net", "netto");
        builder.AddOrUpdate("Admin.Common.TaxPercent", "tax %", "Steuer %");
    }
}