using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Configuration;
using Smartstore.Data.Migrations;
using Smartstore.Utilities;

namespace Smartstore.Core.Data.Migrations;

public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
{
    public DataSeederStage Stage => DataSeederStage.Early;
    public bool AbortOnFailure => false;

    public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        await MigrateSettingsAsync(context, cancelToken);
        await MigrateMessageTemplatesAsync(context, cancelToken);
    }

    public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        var settings = context.Set<Setting>();
        const string oldName1 = "TaxSettings.ShowLegalHintsInProductDetails";
        const string oldName2 = "TaxSettings.ShowLegalHintsInProductList";

        var oldSettings = await settings
            .Where(x => x.Name == oldName1 || x.Name == oldName2)
            .ToListAsync(cancelToken);
        var oldSettings1 = oldSettings.Where(x => x.Name == oldName1).ToList();
        var oldSettings2 = oldSettings.Where(x => x.Name == oldName2).ToList();

        await MigrateLegalInfo(oldSettings1, TypeHelper.NameOf<CatalogSettings>(x => x.LegalInfoInProductDetail, true));
        await MigrateLegalInfo(oldSettings2, TypeHelper.NameOf<CatalogSettings>(x => x.LegalInfoInLists, true));

        if (oldSettings1.Count > 0 || oldSettings2.Count > 0)
        {
            await context.SaveChangesAsync(cancelToken);
        }

        async Task MigrateLegalInfo(List<Setting> oldSettings, string newName)
        {
            if (oldSettings.Count == 0)
            {
                return;
            }

            var storeIds = oldSettings.ToDistinctArray(x => x.StoreId);
            var existingNewSettings = await settings
                .Where(x => x.Name == newName && storeIds.Contains(x.StoreId))
                .Select(x => x.StoreId)
                .ToListAsync(cancelToken);

            foreach (var oldSetting in oldSettings.Where(x => !existingNewSettings.Contains(x.StoreId)))
            {
                settings.Add(new()
                {
                    Name = newName,
                    StoreId = oldSetting.StoreId,
                    Value = (oldSetting.Value.Convert<bool>() ? ProductLegalInfo.All : ProductLegalInfo.None).Convert(string.Empty)
                });
            }

            settings.RemoveRange(oldSettings);
        }
    }

    public async Task MigrateMessageTemplatesAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        var save = false;
        var withdrawalTemplates = await context.MessageTemplates
            .Where(x => x.Name == "Withdrawal.CustomerNotification" || x.Name == "Withdrawal.MerchantNotification" || x.Name == "Withdrawal.ProceedLink")
            .ToListAsync(cancelToken);

        foreach (var template in withdrawalTemplates)
        {
            var modelNames = template.ModelTypes.SplitSafe(',').Distinct().ToArray();
            if (!modelNames.Contains("Order"))
            {
                template.ModelTypes = template.ModelTypes.Grow("Order", ", ");
                save = true;
            }
        }

        if (save)
        {
            await context.SaveChangesAsync(cancelToken);
        }
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
            "The order could not be completed due to an unexpected error. Customer ID: {0}.",
            "Die Bestellung konnte aufgrund eines unerwarteten Fehlers nicht abgeschlossen werden. Kunden-ID: {0}.");

        builder.AddOrUpdate("Order.AlreadyExists",
            "The order has already been created. Customer ID: {0}.",
            "Die Bestellung wurde bereits angelegt. Kunden-ID: {0}.");

        builder.AddOrUpdate("Admin.Configuration.Payment.Methods.NoMatchingCartHash",
            "Cannot recover order. The current cart hash {0} does not match the original cart hash {1}.",
            "Die Bestellung kann nicht wiederhergestellt werden. Der aktuelle Warenkorb-Hash {0} stimmt nicht mit dem ursprünglichen Warenkorb-Hash {1} überein.");

        builder.AddOrUpdate("Admin.Configuration.Payment.Methods.NoMatchingCartTotal",
            "Cannot recover order. The current cart total {0} does not match the paid amount {1}.",
            "Die Bestellung kann nicht wiederhergestellt werden. Der aktuelle Warenkorb-Gesamtbetrag {0} stimmt nicht mit dem bezahlten Betrag {1} überein.");

        builder.AddOrUpdate("Admin.Configuration.Payment.Methods.EnableOrderRecovery",
            "Recover missing orders",
            "Fehlende Aufträge wiederherstellen",
            "Specifies whether a missing order should be recovered when a webhook notification about a successful payment is received.",
            "Legt fest, ob ein fehlender Auftrag wiederhergestellt werden soll, wenn eine Webhook-Nachricht über eine erfolgreiche Zahlung eingeht.");

        builder.AddOrUpdate("Admin.Configuration.Payment.Methods.OrderRecoveryMinAgeSeconds",
            "Recover missing order x seconds after payment",
            "Fehlenden Auftrag x Sekunden nach Zahlung wiederherstellen",
            "Specifies how many seconds must elapse after payment before a missing order is recovered. The value should not be too small, so that the order process has enough time to be completed. The default is 10 seconds.",
            "Legt fest, wie viele Sekunden nach Zahlung vergehen müssen, damit ein fehlender Auftrag wiederhergestellt wird. Der Wert sollte nicht zu klein sein, um dem Bestellvorgang genügend Zeit für den Abschluss zu geben. Standard ist 10 Sekunden.");

        builder.AddOrUpdate("Admin.Configuration.Payment.Methods.OrderRecoveryMaxAgeHours",
            "Recover missing orders only up to x hours after payment",
            "Fehlenden Auftrag nur bis x Stunden nach Zahlung wiederherstellen",
            "Specifies how many hours after a payment is completed a missing order may be automatically recovered. If the payment is older than the specified value, automatic recovery will no longer be performed. 0 means unlimited age. The default is 24 hours.",
            "Legt fest, wie viele Stunden nach Abschluss einer Zahlung ein fehlender Auftrag automatisch wiederhergestellt werden darf. Ist die Zahlung älter als der angegebene Wert, wird keine automatische Wiederherstellung mehr durchgeführt. 0 bedeutet unbegrenztes Alter. Standard ist 24 Stunden.");

        builder.AddOrUpdate("Admin.Catalog.Products.Deleted",
            "The product was moved to the recycle bin.",
            "Das Produkt wurde in den Papierkorb verschoben.");

        builder.AddOrUpdate("ActivityLog.DeleteProduct",
            "Product ('{0}') has been moved to the recycle bin",
            "Produkt ('{0}') in den Papierkorb verschoben");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.RulesCalculateIncludingTax",
            "Cart rules calculate amounts including tax",
            "Warenkorbregeln berechnen Beträge inklusive Steuer",
            "Specifies whether shopping cart rules calculate amounts as inclusive of or exclusive of tax. If not specified (default), the tax settings for the respective shopping cart or customer apply.",
            "Legt fest, ob Warenkorbregeln die Beträge inklusive oder exklusive Umsatzsteuer berechnen. Falls nicht festgelegt (Standard), gelten die Steuereinstellungen des jeweiligen Warenkorbs bzw. Kunden.");

        #region product legal info

        builder.AddOrUpdate("Products.ShippingInfo",
            "plus shipping",
            "zzgl. Versandkosten");

        builder.AddOrUpdate("Products.ShippingInfoUrl",
            "plus <a href=\"{0}\">shipping</a>",
            "zzgl. <a href=\"{0}\">Versandkosten</a>");

        builder.AddOrUpdate("Products.ShippingInfoWithSurcharge",
            "plus shipping and a <b>{0}</b> shipping surcharge",
            "zzgl. Versandkosten und <b>{0}</b> Versandaufschlag");

        builder.AddOrUpdate("Products.ShippingInfoUrlWithSurcharge",
            "plus <a href=\"{0}\">shipping</a> and a <b>{1}</b> shipping surcharge",
            "zzgl. <a href=\"{0}\">Versandkosten</a> und <b>{1}</b> Versandaufschlag");

        builder.AddOrUpdate("Products.ShippingSurchargeInfo",
            "plus <b>{0}</b> shipping surcharge",
            "zzgl. <b>{0}</b> Versandaufschlag");

        builder.AddOrUpdate("Products.FreeShippingInfo",
            "free shipping",
            "versandkostenfrei");

        builder.AddOrUpdate("Products.TaxLegalInfo", "Prices {0}", "Preise {0}");

        builder.AddOrUpdate("Common.AdditionalShippingSurcharge",
            "Plus <b>{0}</b> shipping surcharge",
            "zzgl. <b>{0}</b> Versandaufschlag");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LegalInfoInProductDetail",
            "Legal information on product page",
            "Rechtliche Hinweise auf der Produktseite",
            "Specifies which tax and shipping cost notes are displayed on the product page. If nothing is selected, no note is displayed. An additional shipping charge is always displayed.",
            "Legt fest, welche Hinweise zu Steuer und Versandkosten auf der Produktseite angezeigt werden. Ohne Auswahl wird kein Hinweis angezeigt. Ein Transportzuschlag wird immer angezeigt.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LegalInfoInLists",
            "Legal information in product lists",
            "Rechtliche Hinweise in Produktlisten",
            "Specifies which tax and shipping cost notes are displayed in the list view and in the product comparison. If nothing is selected, no note is displayed.",
            "Legt fest, welche Hinweise zu Steuer und Versandkosten in der Listenansicht und im Produktvergleich angezeigt werden. Ohne Auswahl wird kein Hinweis angezeigt.");

        builder.AddOrUpdate("Enums.ProductLegalInfo.Tax", "Tax note (incl./excl. VAT)", "Steuerhinweis (inkl./zzgl. MwSt.)");
        builder.AddOrUpdate("Enums.ProductLegalInfo.Shipping", "Shipping cost note", "Versandkostenhinweis");

        builder.Delete(
            "Tax.LegalInfoProductDetail",
            "Tax.LegalInfoProductDetail2",
            "Tax.LegalInfoShort",
            "Tax.LegalInfoShort2",
            "Tax.LegalInfoShort3",
            "Admin.Configuration.Settings.Tax.ShowLegalHintsInProductDetails",
            "Admin.Configuration.Settings.Tax.ShowLegalHintsInProductDetails.Hint",
            "Admin.Configuration.Settings.Tax.ShowLegalHintsInProductList",
            "Admin.Configuration.Settings.Tax.ShowLegalHintsInProductList.Hint",
            "Admin.Configuration.Settings.Tax.ShowLegalHintsInProductGrid",
            "Admin.Configuration.Settings.Tax.ShowLegalHintsInProductGrid.Hint");

        #endregion
    }
}