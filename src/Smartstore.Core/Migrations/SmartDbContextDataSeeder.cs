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
            "Admin.Orders.Products.Edit.ExclTax",
            "Admin.Orders.Fields.OrderShippingInclTax.Hint",
            "Admin.Orders.Fields.OrderSubTotalDiscountInclTax.Hint",
            "Admin.Orders.Fields.OrderSubtotalInclTax.Hint",
            "Admin.Orders.Fields.OrderShippingExclTax.Hint",
            "Admin.Orders.Fields.OrderSubTotalDiscountExclTax.Hint",
            "Admin.Orders.Fields.OrderSubtotalExclTax.Hint");

        builder.AddOrUpdate("Admin.Orders.Products.AddNew.UnitPriceInclTax", "Unit price", "Einzelpreis");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.UnitPriceExclTax", "Unit price", "Einzelpreis");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.SubTotalInclTax", "Total", "Gesamt");
        builder.AddOrUpdate("Admin.Orders.Products.AddNew.SubTotalExclTax", "Total", "Gesamt");

        builder.AddOrUpdate("Admin.Orders.Fields.Edit.InclTax", "{0} gross", "{0} brutto");
        builder.AddOrUpdate("Admin.Orders.Fields.Edit.ExclTax", "{0} net", "{0} netto");
        builder.AddOrUpdate("Admin.Common.TaxPercent", "tax %", "Steuer %");

        builder.AddOrUpdate("Admin.Orders.Fields.OrderShippingInclTax", "Order shipping (gross)", "Versandkosten (brutto)");
        builder.AddOrUpdate("Admin.Orders.Fields.OrderShippingExclTax", "Order shipping (net)", "Versandkosten (netto)");

        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubTotalDiscountInclTax", "Order subtotal discount (gross)", "Rabatt für Auftragszwischensumme (brutto)");
        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubTotalDiscountExclTax", "Order subtotal discount (net)", "Rabatt für Auftragszwischensumme (netto)");

        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubtotalInclTax", "Order subtotal (gross)", "Auftragszwischensumme (brutto)");
        builder.AddOrUpdate("Admin.Orders.Fields.OrderSubtotalExclTax", "Order subtotal (net)", "Auftragszwischensumme (netto)");

        builder.AddOrUpdate("Admin.Orders.Products.Total")
            .Value("de", "Gesamt");

        builder.AddOrUpdate("Admin.Common.TaxCalculator.Enable",
            "Enable automatic gross-to-net conversion",
            "Automatische Brutto-/Netto-Umrechnung aktivieren");

        builder.AddOrUpdate("Admin.Common.TaxCalculator.Disable",
            "Disable automatic gross-to-net conversion",
            "Automatische Brutto-/Netto-Umrechnung deaktivieren");

        builder.AddOrUpdate("Admin.Common.TaxCalculator.NoCalculation",
            "An automatic gross-to-net conversion is not possible (e.g. due to varying tax rates).",
            "Eine automatische Brutto-/Netto-Umrechnung ist nicht möglich (z.B. aufgrund unterschiedlicher Steuersätze).");

        builder.AddOrUpdate("Admin.DataExchange.Export.Filter.IsPublished.Hint",
            "Specifies whether only published objects are exported, provided the object has a publishing setting.",
            "Legt fest, ob nur Objekte, die veröffentlicht wurden, exportiert werden, sofern das Objekt eine Einstellung zur Veröffentlichung besitzt.");

        builder.AddOrUpdate("Footer.Info", "Information", "Informationen");

        builder.AddOrUpdate("ReturnCase.WithdrawEntireOrder",
            "I want to withdraw the contract for the entire order:",
            "Ich möchte den Vertrag für die gesamte Bestellung widerrufen:");
        builder.AddOrUpdate("ReturnCase.WithdrawItems",
            "I want to withdraw the contract for the following items:",
            "Ich möchte den Vertrag für die folgenden Artikel widerrufen:");

        builder.AddOrUpdate("ReturnCase.ReturnEntireOrder",
            "I want to return the entire order:",
            "Ich möchte die gesamte Bestellung zurücksenden:");
        builder.AddOrUpdate("ReturnCase.ReturnItems",
            "I want to return the following items:",
            "Ich möchte die folgenden Artikel zurücksenden:");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DisplayAllImagesNumber",
            "Only show selected variant images from this number of images",
            "Ab dieser Bildanzahl nur Bilder der gewählten Variante anzeigen",
            "Once this number of images has been reached, only the product images of the selected variant are displayed. If fewer images are available, all the product images are shown.",
            "Sobald diese Bildanzahl erreicht ist, werden nur noch die Produktbilder der gewählten Variante angezeigt. Bei weniger Bildern werden hingegen alle Produktbilder angezeigt.");

        builder.AddOrUpdate("Order.PlaceOrderError",
            "An unknown error occurred when placing an order for customer {0}.",
            "Bei der Bestellung für den Kunden {0} ist ein unbekannter Fehler aufgetreten.");
        builder.AddOrUpdate("Order.AlreadyExists",
            "An order cannot be created for customer {0} because it already exists.",
            "Für Kunde {0} kann kein Auftrag erstellt werden, da dieser bereits existiert.");
    }
}