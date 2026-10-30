using System.Text.Json;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Configuration;
using Smartstore.Core.Theming;
using Smartstore.Data.Migrations;
using Smartstore.Json;
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
        await context.MigrateSettingsAsync(builder =>
        {
            builder.Add(TypeHelper.NameOf<ThemeSettings>(x => x.KeepSassCompilerInMemory, true), "False");
        });

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

        await context.Settings
            .Where(x => x.Name == "OrderSettings.ReturnRequestReasons" || x.Name == "OrderSettings.ReturnRequestActions")
            .Where(x => x.Value != null && x.Value.Trim() != "" && !x.Value.TrimStart().StartsWith("["))
            .ForEachAsync(x => x.Value = JsonSerializer.Serialize(x.Value.SplitSafe(','), SmartJsonOptions.Default), cancelToken);
        await context.LocalizedProperties
            .Where(x => x.LocaleKeyGroup == "OrderSettings" && (x.LocaleKey == "ReturnRequestReasons" || x.LocaleKey == "ReturnRequestActions"))
            .Where(x => x.LocaleValue != null && x.LocaleValue.Trim() != "" && !x.LocaleValue.TrimStart().StartsWith("["))
            .ForEachAsync(x => x.LocaleValue = JsonSerializer.Serialize(x.LocaleValue.SplitSafe(','), SmartJsonOptions.Default), cancelToken);
        await context.SaveChangesAsync(cancelToken);

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
        builder.AddOrUpdate("Permissions.DisplayName.Dashboard", "Dashboard", "Dashboard");
        builder.AddOrUpdate("Permissions.DisplayName.Widgets", "Widgets", "Widgets");
        builder.AddOrUpdate("Permissions.DisplayName.Sales", "Sales", "Verkäufe");
        builder.AddOrUpdate("Permissions.DisplayName.Customers", "Customers", "Kunden");
        builder.AddOrUpdate("Permissions.DisplayName.IncompleteOrders", "Incomplete orders", "Unvollständige Bestellungen");
        builder.AddOrUpdate("Permissions.DisplayName.Orders", "Order statistics", "Bestellstatistik");
        builder.AddOrUpdate("Permissions.DisplayName.Bestsellers", "Bestsellers", "Bestseller");
        builder.AddOrUpdate("Permissions.DisplayName.LatestOrders", "Latest orders", "Neueste Bestellungen");
        builder.AddOrUpdate("Permissions.DisplayName.RegisteredCustomers", "Customer registrations", "Kundenregistrierungen");
        builder.AddOrUpdate("Permissions.DisplayName.TopCustomers", "Top customers", "Top Kunden");
        builder.AddOrUpdate("Permissions.DisplayName.StoreReport", "Store statistics", "Shop-Statistiken");
        builder.AddOrUpdate("Permissions.DisplayName.NewsFeed", "News", "News");

        builder.AddOrUpdate("Admin.Dashboard", "Dashboard", "Dashboard");
        builder.AddOrUpdate("Admin.Dashboard.Customize", "Customize dashboard", "Dashboard anpassen");
        builder.AddOrUpdate("Admin.Header.Account", "Account", "Konto");
        builder.AddOrUpdate("Admin.Header.ExpandNavigation", "Expand navigation", "Navigation ausklappen");
        builder.AddOrUpdate("Admin.Header.CollapseNavigation", "Collapse navigation", "Navigation einklappen");
        builder.AddOrUpdate("Admin.Header.ExpandNavigation.Short", "Expand", "Ausklappen");
        builder.AddOrUpdate("Admin.Header.CollapseNavigation.Short", "Collapse", "Einklappen");
        builder.AddOrUpdate("Admin.Configuration.Settings.StoreScope.OwnValues",
            "Break inheritance",
            "Vererbung aufheben",
            "Use separate values for all settings on this page in this store.",
            "Alle Einstellungen dieser Seite für diesen Shop separat festlegen.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturersOnHomepage", "Show manufacturers on the homepage", "Hersteller auf der Startseite anzeigen");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ManufacturerItemsToDisplayOnHomepage",
            "Maximum number of manufacturers on the homepage",
            "Maximale Herstelleranzahl auf der Startseite",
            "Specifies the maximum number of manufacturers displayed on the homepage.",
            "Legt fest, wie viele Hersteller höchstens auf der Startseite angezeigt werden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturersInOffCanvas",
            "Show manufacturers in the OffCanvas menu",
            "Hersteller im OffCanvas-Menü anzeigen",
            "Specifies whether manufacturers are displayed in the OffCanvas menu. This is the navigation menu that slides in from the side and that customers open using the menu button on mobile devices.",
            "Legt fest, ob Hersteller im OffCanvas-Menü angezeigt werden. Das ist das seitlich eingeblendete Navigationsmenü, das Kunden über die Menü-Schaltfläche auf mobilen Endgeräten öffnen.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ManufacturerItemsToDisplayInOffcanvasMenu",
            "Maximum number of manufacturers in the OffCanvas menu",
            "Maximale Herstelleranzahl im OffCanvas-Menü",
            "Specifies the maximum number of manufacturers displayed in the OffCanvas menu.",
            "Legt fest, wie viele Hersteller höchstens im OffCanvas-Menü angezeigt werden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerPictures.Hint",
            "Specifies whether manufacturer images are displayed in manufacturer lists on the homepage and in the OffCanvas menu.",
            "Legt fest, ob Herstellerbilder in den Herstellerlisten auf der Startseite und im OffCanvas-Menü angezeigt werden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.HideManufacturerDefaultPictures", "Hide manufacturer placeholder images", "Hersteller-Platzhalterbilder ausblenden");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.SortManufacturersAlphabetically", "Sort manufacturer overview alphabetically", "Herstellerübersicht alphabetisch sortieren");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.AllowDifferingEmailAddressForEmailAFriend",
            "Allow a different email address",
            "Abweichende E-Mail-Adresse erlauben",
            "Specifies whether customers can change their own email address in the product recommendation form. Otherwise, the address from their customer account is used and cannot be edited. Enable this option also if guests are allowed to recommend products by email.",
            "Legt fest, ob Kunden ihre eigene E-Mail-Adresse im Weiterempfehlungsformular ändern dürfen. Andernfalls wird die Adresse aus dem Kundenkonto verwendet und kann nicht bearbeitet werden. Aktivieren Sie diese Option auch, wenn Gäste Produkte per E-Mail weiterempfehlen dürfen.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.NotifyStoreOwnerAboutNewProductReviews",
            "Notify about new reviews by email",
            "Über neue Bewertungen per E-Mail informieren",
            "Specifies whether the store owner is notified by email when a new product review is submitted.",
            "Legt fest, ob der Shop-Betreiber bei einer neuen Produktbewertung per E-Mail benachrichtigt wird.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ReviewsDisplay", "Show reviews…", "Bewertungen anzeigen…");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductReviewsInProductLists", "In product lists", "In Produktlisten");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductReviewsInProductDetail", "On the product detail page", "Auf der Produktdetailseite");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ProductReviewsGroup", "Product reviews", "Produktbewertungen");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ProductContactGroup", "Product inquiries & recommendations", "Produktanfragen & Weiterempfehlungen");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.EmailAFriendEnabled",
            "Enable product recommendations by email",
            "Weiterempfehlung per E-Mail aktivieren",
            "Specifies whether customers can recommend a product by email.",
            "Legt fest, ob Kunden ein Produkt per E-Mail weiterempfehlen können.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowOfferCountdownRemainingHours",
            "Show countdown before offer ends",
            "Countdown vor Angebotsende anzeigen",
            "Specifies how many hours before an offer ends a countdown is displayed on the product detail page and in the shopping cart. A value of 72 hours displays it during the last three days. Only applies to offers with an end date. Leave the field empty to disable the countdown. A setting on the discount takes precedence.",
            "Legt fest, wie viele Stunden vor Angebotsende ein Countdown auf der Produktdetailseite und im Warenkorb angezeigt wird. Bei 72 Stunden erscheint er während der letzten drei Tage. Gilt nur für Angebote mit Enddatum. Lassen Sie das Feld leer, um den Countdown zu deaktivieren. Eine eigene Einstellung am Rabatt hat Vorrang.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ApplyTierPricePercentageToAttributePriceAdjustments",
            "Apply tier discount to variant surcharges and reductions",
            "Staffelrabatt auf Variantenauf- und -abschläge anwenden",
            "Specifies whether percentage tier discounts also apply to price surcharges and reductions for variant options. For example, a 10% tier discount reduces a variant surcharge from €5.00 to €4.50. Applies to order quantities greater than 1 and not to variant options linked to products.",
            "Legt fest, ob prozentuale Staffelermäßigungen auch auf Preisaufschläge und Preisabschläge von Variantenoptionen angewendet werden. Beispielsweise reduziert eine Staffelermäßigung von 10 % einen Variantenaufschlag von 5,00 € auf 4,50 €. Gilt für Bestellmengen größer als 1 und nicht für Variantenoptionen mit Produktverknüpfung.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.CheckListPrices", "Check when displaying list prices…", "Bei Listenpreisen prüfen…");
        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountLimitationsInLists", "Discount usage limits", "Rabatt-Nutzungsgrenzen");
        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountRulesInLists", "Cart rules for discounts", "Warenkorbregeln von Rabatten");
        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountGiftCardsInLists",
            "Gift cards in the cart",
            "Geschenkgutscheine im Warenkorb",
            "Checks for gift card products in the cart. The current implementation only rejects discounts on the order total or subtotal when gift cards are present. Regular product list price calculation processes product, category and manufacturer discounts, so this check currently has no effect there. This does not refer to redeemed coupon codes.",
            "Prüft auf Geschenkgutschein-Produkte im Warenkorb. Der vorhandene Code schließt dabei nur Rabatte auf die Bestell- oder Zwischensumme aus. Da die normale Produktlistenberechnung Produkt-, Warengruppen- und Markenrabatte verarbeitet, hat diese Prüfung dort derzeit keine Wirkung. Gemeint sind keine eingelösten Rabattcodes.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.IgnoreDiscounts",
            "Disable discounts",
            "Rabatte deaktivieren",
            "Specifies whether configured discounts are disregarded during price calculation. Special prices, tier prices and discounts on bundle items are not affected.",
            "Legt fest, ob eingerichtete Rabatte bei der Preisberechnung unberücksichtigt bleiben. Aktionspreise, Staffelpreise und Rabatte auf Bundle-Bestandteile sind davon nicht betroffen.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowLoginForPriceNote.Hint",
            "Specifies whether the product detail page displays the message 'Prices are displayed after login.' when the visitor is not allowed to see prices. To display prices only to logged-in customers, revoke the price display permission for the 'Guests' customer group under 'Access control' and grant it to registered customers.",
            "Legt fest, ob auf der Produktdetailseite der Hinweis „Preise werden nach Anmeldung angezeigt.“ erscheint, wenn der Besucher keine Preise sehen darf. Um Preise nur angemeldeten Kunden anzuzeigen, entziehen Sie der Kundengruppe „Gäste“ unter „Zugriffsrechte“ das Recht zur Preisanzeige und erlauben es für registrierte Kunden.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowVariantCombinationPriceAdjustment",
            "Show surcharges and reductions for variants",
            "Auf- und Abschläge bei Varianten anzeigen",
            "Specifies whether price surcharges and reductions are displayed for variant options, for example 'Extra large (+ €5.00)'. This setting only affects the display; surcharges and reductions are still included in the product price.",
            "Legt fest, ob Preisaufschläge und Preisabschläge bei Variantenoptionen angezeigt werden, beispielsweise „Extra groß (+ 5,00 €)“. Die Einstellung betrifft nur die Anzeige; die Auf- und Abschläge werden weiterhin im Produktpreis berücksichtigt.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ProductsAlsoPurchasedEnabled",
            "Enable 'Also purchased'",
            "„Auch gekauft“ aktivieren",
            "Specifies whether products ordered together with this product are displayed on the product detail page.",
            "Legt fest, ob auf der Produktdetailseite Produkte angezeigt werden, die zusammen mit diesem Produkt bestellt wurden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ProductsAlsoPurchasedNumber",
            "Maximum number of products",
            "Maximale Produktanzahl",
            "Specifies the maximum number of products displayed in the 'Also purchased' section.",
            "Legt fest, wie viele Produkte im Bereich „Auch gekauft“ höchstens angezeigt werden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.RecentlyViewedProductsEnabled",
            "Enable 'Recently viewed'",
            "„Zuletzt angesehen“ aktivieren",
            "Specifies whether customers can see their recently viewed products on a dedicated overview page and in additional product overviews throughout the store.",
            "Legt fest, ob Kunden ihre zuletzt angesehenen Produkte auf einer eigenen Übersichtsseite und in ergänzenden Produktübersichten im Shop sehen können.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.RecentlyViewedProductsNumber",
            "Maximum number of products",
            "Maximale Produktanzahl",
            "Specifies the maximum number of products displayed in the 'Recently viewed' section.",
            "Legt fest, wie viele Produkte im Bereich „Zuletzt angesehen“ höchstens angezeigt werden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.RecentlyAddedProductsEnabled",
            "Enable 'New arrivals'",
            "„Neu eingetroffen“ aktivieren",
            "Specifies whether customers can view the most recently added products on the 'New arrivals' page.",
            "Legt fest, ob Kunden die zuletzt hinzugefügten Produkte auf der Seite „Neu eingetroffen“ ansehen können.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.RecentlyAddedProductsNumber",
            "Maximum number of products",
            "Maximale Produktanzahl",
            "Specifies the maximum number of products displayed in the 'New arrivals' section.",
            "Legt fest, wie viele Produkte im Bereich „Neu eingetroffen“ höchstens angezeigt werden.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowLinkedAttributeValueQuantity",
            "Show product quantity in variant selection",
            "Produktmenge in der Variantenauswahl anzeigen",
            "Specifies whether variant options linked to a product display the configured quantity before the name, for example '2 x spare battery'. The quantity is only displayed when it is greater than 1 and the customer is allowed to see prices.",
            "Legt fest, ob bei Variantenoptionen mit Produktverknüpfung die hinterlegte Stückzahl vor dem Namen angezeigt wird, beispielsweise „2 x Ersatzakku“. Die Angabe erscheint nur bei Mengen größer als 1 und wenn der Kunde Preise sehen darf.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowLinkedAttributeValueImage",
            "Show product image in variant selection",
            "Produktbild in der Variantenauswahl anzeigen",
            "Specifies whether variant options linked to a product use the linked product's first image as a thumbnail. The display depends on the variant attribute's control type.",
            "Legt fest, ob bei Variantenoptionen mit Produktverknüpfung das erste Bild des verknüpften Produkts als Vorschaubild verwendet wird. Die Darstellung hängt vom Auswahltyp des Variantenattributs ab.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.AdditionalProductOverviews", "Additional product overviews", "Weitere Produktübersichten");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductCondition", "Item condition", "Artikelzustand");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductTags", "Tags", "Tags");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerInProductDetail", "Manufacturer/brand", "Hersteller/Marke");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerPicturesInProductDetail", "Brand logo", "Markenlogo");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowShortDescriptionInGridStyleLists", "Short description", "Kurzbeschreibung");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerInGridStyleLists", "Manufacturer/brand", "Hersteller/Marke");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductOptionsInLists", "Variant names", "Variantnamen");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowColorSquaresInLists", "Color variants", "Farbvarianten");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerLogoInLists", "Brand logo instead of brand name", "Markenlogo statt Markenname");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LinkManufacturerLogoInLists", "Link brand logo", "Markenlogo verlinken");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.IgnoreFeaturedProducts",
            "Hide featured products block",
            "Top-Produkte-Block ausblenden",
            "Disables the separate featured products block on category and manufacturer pages. Whether featured products appear in the regular product list is controlled by 'Show featured products in lists'.",
            "Deaktiviert den separaten Top-Produkte-Block auf Warengruppen- und Markenseiten. Ob Top-Produkte in der normalen Produktliste erscheinen, wird durch „Top-Produkte in Listen anzeigen“ festgelegt.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.IncludeInCompareProducts", "Include in product comparisons…", "Beim Produktvergleich einbeziehen…");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.IncludeShortDescriptionInCompareProducts", "Short description", "Kurzbeschreibung");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.IncludeFullDescriptionInCompareProducts", "Full description", "Komplette Beschreibung");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductInformation", "Show product information…", "Produktinformationen anzeigen…");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductSku",
            "SKU",
            "Artikelnummer",
            "Specifies whether SKUs are displayed on product detail pages, in the list view of product lists, in product comparisons, in the shopping cart, on the wishlist and during checkout. Also applies to order and shipment details and printed orders and packing slips.",
            "Legt fest, ob Artikelnummern auf Produktdetailseiten, in der Listenansicht von Produktlisten, im Produktvergleich, im Warenkorb, auf der Merkliste und im Bestellvorgang angezeigt werden. Die Einstellung gilt auch für Bestell- und Versanddetails sowie die Druckansicht von Bestellungen und Lieferscheinen.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerPartNumber",
            "Manufacturer part number",
            "Herstellerartikelnummer",
            "Check the box to show manufacturer part numbers in public store.",
            "Legt fest, ob die Hersteller-Produktnummer im Shop angezeigt werden soll.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowGtin",
            "GTIN",
            "GTIN",
            "Check the box to show GTIN in public store.",
            "Legt fest, ob die EAN Nummer im Shop angezeigt werden soll.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowWeight",
            "Weight",
            "Gewicht",
            "Specifies whether the product weight is displayed on the product detail page, in the list view of product lists and in product comparisons. Weight display in the shopping cart is configured separately in the shopping cart settings.",
            "Legt fest, ob das Produktgewicht auf der Produktdetailseite, in der Listenansicht von Produktlisten und im Produktvergleich angezeigt wird. Die Gewichtsanzeige im Warenkorb wird separat in den Warenkorb-Einstellungen festgelegt.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowDimensions",
            "Dimensions",
            "Abmessungen",
            "Specifies whether width, height and length are displayed on the product detail page, in the list view of product lists and in product comparisons.",
            "Legt fest, ob Breite, Höhe und Länge auf der Produktdetailseite, in der Listenansicht von Produktlisten und im Produktvergleich angezeigt werden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowDefaultQuantityUnit",
            "Use default quantity unit",
            "Standard-Verpackungseinheit verwenden",
            "Specifies whether the default quantity unit is used when no quantity unit is assigned to the product.",
            "Legt fest, ob die Standard-Verpackungseinheit verwendet wird, wenn dem Produkt keine Verpackungseinheit zugewiesen ist.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowDefaultDeliveryTime",
            "Use default delivery time",
            "Standard-Lieferzeit verwenden",
            "Specifies whether the default delivery time is used when no delivery time is assigned to the product.",
            "Legt fest, ob die Standard-Lieferzeit verwendet wird, wenn dem Produkt keine Lieferzeit zugewiesen ist.");

        builder.AddOrUpdate("Admin.CatalogSettings.ProductDisplay", "Shared product settings", "Übergreifende Produkteinstellungen");
        builder.AddOrUpdate("Admin.CatalogSettings.ProductDisplay.Info",
            "These settings are used in multiple places in the store. For example, the SKU display setting also controls its display on orders; the placeholder image setting applies to products and the shopping cart, among other places.",
            "Diese Einstellungen werden an mehreren Stellen im Shop verwendet. Beispielsweise steuert die Artikelnummer-Anzeige auch die Darstellung bei Bestellungen; die Einstellung für Platzhalterbilder wirkt unter anderem bei Produkten und im Warenkorb.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultProductListPageSize",
            "Products per page",
            "Produkte pro Seite",
            "Specifies how many products are displayed on a product list page.",
            "Legt fest, wie viele Produkte auf einer Produktlistenseite angezeigt werden.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ProductTileContent", "Product tile content", "Inhalt der Produktkacheln");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.AllowProductViewModeChanging.Hint",
            "Displays a switch between grid and list views in product lists for customers. The initial view is set with 'Default view mode'.",
            "Zeigt Kunden einen Umschalter zwischen Raster- und Listenansicht in Produktlisten an. Die anfängliche Darstellung legen Sie mit „Standardansicht“ fest.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.KeepOnSubPages",
            "Keep when paging and filtering…",
            "Beim Blättern und Filtern beibehalten…",
            "Specifies which content remains visible from page 2 of the product list onwards and when filters are active.",
            "Legt fest, welche Inhalte ab Seite 2 der Produktliste und bei aktiven Filtern weiterhin angezeigt werden.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.IncludeFeaturedProductsInSubPages", "Featured products", "Top-Produkte");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowSubCategoriesInSubPages",
            "Subcategories",
            "Unterwarengruppen",
            "Keeps subcategories visible from page 2 onwards and when filters are active. Only applies when subcategories are displayed above the product list.",
            "Zeigt Unterwarengruppen auch ab Seite 2 der Produktliste und bei aktiven Filtern an. Gilt nur für die Anzeige oberhalb der Produktliste.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowDescriptionInSubPages",
            "Page descriptions",
            "Seitenbeschreibungen",
            "Keeps category and manufacturer descriptions visible from page 2 onwards and when filters are active.",
            "Zeigt Beschreibungen von Warengruppen und Marken auch ab Seite 2 der Produktliste und bei aktiven Filtern an.");

        builder.Delete(
            "Admin.Catalog.Products.Fields.Published.Hint",
            "Admin.Catalog.Products.Fields.QuantityUnit.Hint",
            "Admin.Catalog.Products.Fields.Weight.Hint",
            "Admin.Catalog.Products.Fields.Length.Hint",
            "Admin.Catalog.Products.Fields.Width.Hint",
            "Admin.Catalog.Products.Fields.Height.Hint",
            "Admin.Catalog.Products.Groups.RequiredProducts",
            "Admin.Catalog.Products.Groups.GiftCard",
            "Admin.Catalog.Products.Groups.Subscription",
            "Admin.Catalog.Products.Availability.From",
            "Admin.Catalog.Products.Availability.To");

        builder.AddOrUpdate("Admin.Common.Store.LimitedTo",
            "Limited to stores",
            "Auf Shops begrenzt",
            "Specifies whether the object is only available in certain stores.",
            "Legt fest, ob das Objekt nur für bestimmte Shops verfügbar ist.");

        builder.AddOrUpdate("Admin.Catalog.Products.Fields.HomePageDisplayOrder", "Homepage display order", "Reihenfolge auf der Homepage");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.DisplayOrder",
            "General sort priority",
            "Allgemeine Sortierpriorität",
            "Specifies the order in which associated products of a grouped product are displayed. In addition, this setting determines the order of hits in the search, if sort by recommendation is enabled in the search settings. Lower values are displayed first.",
            "Legt die Reihenfolge fest, in der verknüpfte Produkte eines Gruppenproduktes angezeigt werden. Zusätzlich legt diese Einstellung die Reihenfolge der Treffer bei der Suche fest, sofern in den Sucheinstellungen die Sortierung nach Empfehlung aktiviert ist. Kleinere Werte werden zuerst angezeigt.");
        builder.AddOrUpdate("Admin.Catalog.Products.Groups.Restrictions", "Publication and availability", "Veröffentlichung und Verfügbarkeit");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.Availability",
            "Availability",
            "Verfügbarkeit",
            "Limits product availability to this period. Leave either field empty to leave that boundary unrestricted.",
            "Begrenzt die Verfügbarkeit des Produkts auf diesen Zeitraum. Lassen Sie ein Feld leer, um die jeweilige Grenze offen zu lassen.");
        builder.AddOrUpdate("Admin.Catalog.Products.Groups.Taxes", "Taxes", "Steuern");
        builder.AddOrUpdate("Admin.Catalog.Products.Groups.SalesOptions", "Sales options", "Verkaufsoptionen");
        builder.AddOrUpdate("Admin.Catalog.Products.Groups.SalesOptions.Info",
            "Specify whether additional items must be ordered with this product or whether it is sold as a gift card or subscription. Enable an option to show its settings.",
            "Hier legen Sie fest, ob zum Produkt weitere Artikel mitbestellt werden müssen oder ob es als Geschenkgutschein oder Abonnement verkauft wird. Aktivieren Sie eine Option, um die zugehörigen Einstellungen einzublenden.");
        builder.AddOrUpdate("Admin.Catalog.Products.Groups.Shipping", "Shipping, dimensions and units", "Versand, Maße und Einheiten");
        builder.AddOrUpdate("Admin.Catalog.Products.Groups.Shipping.Info",
            "The quantity unit specifies how product quantities are labeled in the shop, for example as pieces, packs or pairs. It appears alongside the quantity when ordering and in the cart. Weight and dimensions can affect shipping costs, depending on the shipping provider, and are displayed on the product page when the corresponding catalog settings are enabled.",
            "Die Verpackungseinheit legt fest, wie Produktmengen im Shop bezeichnet werden, zum Beispiel als Stück, Packung oder Paar. Sie erscheint bei der Mengenauswahl und im Warenkorb neben der Anzahl. Gewicht und Maße können je nach Versandanbieter die Versandkosten beeinflussen und werden bei aktivierten Katalogeinstellungen auf der Produktseite angezeigt.");

        builder.AddOrUpdate("Admin.Catalog.Products.Fields.AllowCustomerReviews",
            "Allow customer reviews",
            "Kundenbewertungen erlauben",
            "Allows customers to review this product. To display reviews on the product page, enable 'On the product detail page' under 'Show reviews…' in the catalog settings.",
            "Legt fest, ob Kunden Produktbewertungen vornehmen dürfen. Damit Bewertungen auf der Produktseite angezeigt werden, muss in den Katalogeinstellungen unter „Bewertungen anzeigen…“ die Option „Auf der Produktdetailseite“ aktiviert sein.");

        builder.AddOrUpdate("Admin.Catalog.Products.Fields.Condition",
            "Item condition",
            "Artikelzustand",
            "Condition of the goods offered, e.g. new, used or refurbished. Used for structured product data and supported product exports. Display in the shop is controlled by the catalog settings.",
            "Zustand der angebotenen Ware, z. B. neu, gebraucht oder generalüberholt. Wird für strukturierte Produktdaten und unterstützte Produktexporte verwendet. Die Anzeige im Shop wird über die Katalogeinstellungen gesteuert.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.CountryOfOriginId",
            "Country of origin",
            "Herkunftsland",
            "Country of origin of the goods. This information is available to integrations and exports. It does not determine tax or shipping costs in the standard implementation.",
            "Ursprungsland der Ware. Die Angabe steht für Schnittstellen und Exporte zur Verfügung. Sie bestimmt im Standard keine Steuer- oder Versandkosten.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.IsTaxExempt",
            "Exempt from VAT",
            "Von der Umsatzsteuer befreit",
            "No VAT is calculated for this product during checkout.",
            "Für dieses Produkt wird im Bestellvorgang keine Umsatzsteuer berechnet.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.TaxCategory",
            "Tax category",
            "Steuerkategorie",
            "Select the product's tax category. The tax provider determines the applicable rate based on this category and the tax address.",
            "Wählen Sie die Steuerkategorie des Produkts. Der Steueranbieter ermittelt daraus und anhand der Steueradresse den anzuwendenden Steuersatz.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.IsEsd",
            "Electronically supplied service",
            "Elektronisch erbrachte Dienstleistung",
            "When EU VAT handling is enabled, the billing address is used as the tax address for EU consumers. This setting does not enable downloadable products.",
            "Bei aktivierter EU-Umsatzsteuerbehandlung wird für EU-Verbraucher die Rechnungsadresse als Steueradresse verwendet. Diese Einstellung aktiviert keine Downloadbereitstellung.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.RequireOtherProducts",
            "Additional products must be ordered",
            "Weitere Produkte müssen mitbestellt werden",
            "The selected additional products must be ordered together with this product. Enable automatic addition to add them to the cart automatically.",
            "Die ausgewählten Zusatzprodukte müssen zusammen mit diesem Produkt bestellt werden. Aktivieren Sie das automatische Hinzufügen, um sie automatisch in den Warenkorb zu legen.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.IsGiftCard",
            "Sell as a gift card",
            "Als Geschenkgutschein verkaufen",
            "A gift card is created when this product is ordered. The gift card type determines whether it is virtual or physical.",
            "Bei Bestellung dieses Produkts wird ein Geschenkgutschein erstellt. Der Gutscheintyp legt fest, ob es sich um einen virtuellen oder physischen Gutschein handelt.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.IsRecurring",
            "Sell as a subscription",
            "Als Abonnement verkaufen",
            "Enables recurring payments for this product. Set the billing interval and number of cycles below. Requires a payment method that supports recurring payments.",
            "Aktiviert wiederkehrende Zahlungen für dieses Produkt. Legen Sie darunter das Zahlungsintervall und die Anzahl der Zyklen fest. Erfordert eine Zahlungsart, die wiederkehrende Zahlungen unterstützt.");
        builder.AddOrUpdate("Admin.Catalog.Products.Fields.IsShipEnabled",
            "Shipping required",
            "Versand erforderlich",
            "Enable if this product must be shipped. The product is included in shipping calculations. When disabled, delivery times are hidden in the shop. The quantity unit is independent of this setting.",
            "Aktivieren, wenn dieses Produkt versendet werden muss. Es wird bei der Versandberechnung berücksichtigt. Bei deaktivierter Option werden Lieferzeiten im Shop ausgeblendet. Die Verpackungseinheit ist davon unabhängig.");

        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Title", "Migrate icons", "Icons migrieren");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Start", "Start migration", "Migration starten");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Hint",
            "Converts legacy Font Awesome icons in menus and HTML content to the new icon format. Already migrated icons remain unchanged. Migration usually takes just a few seconds, but may take several minutes for large datasets. Keep this page open until the migration finishes. Logs of automatic and manual migrations are available in App_Data/Logs (icon-migration-*).",
            "Überführt bisherige Font-Awesome-Icons in Menüs und HTML-Inhalten in das neue Icon-Format. Bereits migrierte Icons bleiben unverändert. Die Migration dauert meist nur wenige Sekunden, bei großen Datenbeständen auch mehrere Minuten. Lassen Sie diese Seite bis zum Abschluss geöffnet. Protokolle automatischer und manueller Migrationen finden Sie unter App_Data/Logs (icon-migration-*).");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Started", "Icon migration is running…", "Die Icon-Migration läuft…");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Completed",
            "Completed: {0} records checked, {1} icons migrated, {2} icons not migrated.",
            "Abgeschlossen: {0} Datensätze geprüft, {1} Icons migriert, {2} Icons nicht migriert.");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Failed",
            "The migration could not be started or the connection was interrupted. You can start it again.",
            "Die Migration konnte nicht gestartet werden oder die Verbindung wurde unterbrochen. Sie können sie erneut starten.");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Migrated", "Migrated", "Migriert");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Warning", "Warning", "Hinweis");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.NotMigrated", "Not migrated", "Nicht migriert");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Columns.Entity", "Entity", "Entität");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Columns.RecordId", "Record ID", "Datensatz-ID");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Columns.Field", "Field", "Feld");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Columns.Message", "Message", "Meldung");
        builder.AddOrUpdate("Admin.System.Maintenance.Icons.Columns.NewValue", "New icon value", "Neuer Icon-Wert");

        builder.AddOrUpdate("Smartstore.AI.Prompts.UseImagePlaceholders",
            "If an image is to be inserted, output this HTML placeholder: <div class=\"mb-3\"><sm-icon name=\"file-image\" size=\"xl\" class=\"ai-preview-file\" title=\"SECTION_HEADING\"></sm-icon></div>. Replace SECTION_HEADING with the associated section heading, escaped for an HTML attribute. Keep the element names and all other attributes exactly as shown. The sm-icon element must remain empty and have an explicit closing tag; do not replace it with an i, img or svg element or add Font Awesome classes.",
            "Wenn ein Bild einzufügen ist, gib diesen HTML-Platzhalter aus: <div class=\"mb-3\"><sm-icon name=\"file-image\" size=\"xl\" class=\"ai-preview-file\" title=\"ABSCHNITTSÜBERSCHRIFT\"></sm-icon></div>. Ersetze ABSCHNITTSÜBERSCHRIFT durch die zugehörige Abschnittsüberschrift, korrekt für ein HTML-Attribut maskiert. Behalte die Elementnamen und alle anderen Attribute exakt bei. Das sm-icon-Element muss leer bleiben und ein explizites schließendes Tag haben; ersetze es nicht durch ein i-, img- oder svg-Element und ergänze keine Font-Awesome-Klassen.");

        builder.AddOrUpdate("ShoppingCart.Mini.EmptyCart.Info",
            "You have not added any product to your cart yet. Use the {0} icon to add a product to your cart.",
            "Sie haben noch keine Produkte in Ihren Warenkorb gelegt.<br /> Benutzen Sie das {0} Symbol, um ein Produkt in den Warenkorb zu legen.");
        builder.AddOrUpdate("ShoppingCart.Mini.EmptyWishlist.Info",
            "You have not added any product to your wishlist yet. Use the {0} icon to add a product to your wishlist.",
            "Sie haben noch keine Produkte auf Ihrer Wunschliste vermerkt.<br /> Benutzen Sie das {0} Symbol, um ein Produkt in Ihrer Wunschliste zu vermerken.");
        builder.AddOrUpdate("ShoppingCart.Mini.EmptyCompare.Info",
            "You have not added any product to your compare list yet. Use the {0} icon to add a product to your compare list.",
            "Sie haben noch keine Produkte in Ihrer Vergleichsliste.<br /> Benutzen Sie das {0} Symbol, um ein Produkt in die Vergleichsliste aufzunehmen.");

        builder.AddOrUpdate("Common.Select.AddPlaceholder", "＋ Add…", "＋ Hinzufügen…");

        builder.AddOrUpdate("Enums.QuantizationMethod.Octree", "Hexadecatree (formerly Octree)", "Hexadecatree (vormals Octree)");
        builder.AddOrUpdate("Enums.JpegColorType.YCbCrRatio422", "YCbCr 4:2:2", "YCbCr 4:2:2");
        builder.AddOrUpdate("Enums.JpegColorType.YCbCrRatio411", "YCbCr 4:1:1", "YCbCr 4:1:1");
        builder.AddOrUpdate("Enums.JpegColorType.YCbCrRatio410", "YCbCr 4:1:0", "YCbCr 4:1:0");
        builder.AddOrUpdate("Admin.Configuration.Settings.Media.GifQuantizationMethod", "GIF palette quantization", "GIF-Palettenquantisierung");

        builder.AddOrUpdate("Admin.Configuration.Settings.Media.JpegColorType",
            "JPEG color type",
            "JPEG-Farbtyp",
            "Leave empty for automatic selection.",
            "Leer lassen für automatische Auswahl.");
        builder.AddOrUpdate("Admin.Configuration.Settings.Media.PngQuantizationMethod",
            "PNG palette quantization",
            "PNG-Palettenquantisierung",
            "Only affects palette-based PNG images. Has no effect on RGB or RGBA images.",
            "Wirkt nur auf PNG-Bilder mit Farbpalette. Hat bei RGB- und RGBA-Bildern keine Wirkung.");

        builder.AddOrUpdate("Admin.IconBrowser.SelectSource", "Select source", "Quelle wählen",
            "Select an icon source", "Icon-Quelle wählen");
        builder.AddOrUpdate("Admin.IconBrowser.Kits", "Kits", "Kits");
        builder.AddOrUpdate("Admin.IconBrowser.Libraries", "Libraries", "Bibliotheken");
        builder.AddOrUpdate("Admin.IconBrowser.KitTitle", "Kit: {0}", "Kit: {0}");
        builder.AddOrUpdate("Admin.IconBrowser.LibraryTitle", "Library: {0}", "Bibliothek: {0}");
        builder.AddOrUpdate("Admin.IconBrowser.Icon", "Icon", "Icon");
        builder.AddOrUpdate("Admin.IconBrowser.Search", "Search icons", "Icons suchen");
        builder.AddOrUpdate("Admin.IconBrowser.Size", "Size", "Größe");
        builder.Delete("Admin.IconBrowser.Color", "Admin.IconBrowser.FlipHorizontal", "Admin.IconBrowser.FlipVertical", "Admin.IconBrowser.Rotation", "Admin.IconBrowser.Animation", "Admin.IconBrowser.StrokeWidth", "Admin.IconBrowser.NoAnimation");
        builder.AddOrUpdate("Common.MoreOptions", "More options", "Weitere Optionen");
        builder.AddOrUpdate("Admin.Catalog.Categories.Fields.Icon",
            "Icon",
            "Icon",
            "The icon appears in the category navigation, in the main menu including dropdowns, in the mobile menu and in the category tree in the administration.",
            "Das Icon erscheint in der Kategorienavigation, im Hauptmenü einschließlich Dropdowns, im mobilen Menü sowie im Kategoriebaum der Administration.");

        builder.AddOrUpdate("Admin.Design.Color", "Color", "Farbe");
        builder.AddOrUpdate("Admin.Design.Opacity", "Opacity", "Deckkraft");
        builder.AddOrUpdate("Admin.Design.FlipHorizontal", "Flip horizontally", "Horizontal spiegeln");
        builder.AddOrUpdate("Admin.Design.FlipVertical", "Flip vertically", "Vertikal spiegeln");
        builder.AddOrUpdate("Admin.Design.Rotation", "Rotation", "Rotation");
        builder.AddOrUpdate("Admin.Design.Animation", "Animation", "Animation");
        builder.AddOrUpdate("Admin.Design.StrokeWidth", "Stroke width", "Strichstärke");
        builder.AddOrUpdate("Admin.Design.NoAnimation", "No animation", "Keine Animation");
        builder.AddOrUpdate("Admin.IconBrowser.Options", "Icon options", "Icon-Optionen");
        builder.AddOrUpdate("Admin.IconBrowser.External", "provided by a module", "von einem Modul bereitgestellt");

        builder.AddOrUpdate("Admin.Themes.Info.Flex",
            "A clean, versatile storefront with bright surfaces, clear typography and a product-focused layout. The flexible foundation for the Flex theme family.",
            "Ein klarer, vielseitiger Shop mit hellen Flächen, gut lesbarer Typografie und einem Layout, das Ihre Produkte in den Mittelpunkt stellt. Die flexible Basis der Flex-Theme-Familie.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexBlack",
            "A distinctive boxed storefront with slim Barlow Semi Condensed typography, a black service bar and layered graphite navigation. Crisp white content and soft neutral panels create a restrained, contemporary contrast.",
            "Ein markantes Box-Layout mit schlanker Barlow-Semi-Condensed-Typografie, schwarzer Serviceleiste und abgestuften Graphittönen in der Navigation. Weiße Inhaltsflächen und sanfte Neutraltöne sorgen für einen zurückhaltenden, modernen Kontrast.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexBlue",
            "A signature all-blue header, crisp white product surfaces and warm orange actions. Soft blue details and a deep navy footer give this classic a clear, contemporary feel.",
            "Ein durchgehend blauer Header, klare weiße Produktflächen und warme orangefarbene Akzente. Sanfte Blautöne und ein tiefblauer Footer verleihen diesem Klassiker einen zeitgemäßen Charakter.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexSorbet",
            "A fresh editorial palette of aubergine, lime and mint on warm porcelain, with expressive serif headings.",
            "Eine frische Kombination aus Aubergine, Limette und Mint auf warmem Porzellanweiß. Ausdrucksstarke Serifenschriften setzen die Überschriften in Szene.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexTidal",
            "Atlantic ink, vivid coral and glacial blue. Crisp typography, clean geometry and bright product surfaces.",
            "Atlantikblau, lebendiges Korallrot und kühles Eisblau. Klare Typografie, geradlinige Formen und helle Produktflächen.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexAfterglow",
            "Graphite nights with electric mint, soft orchid and apricot accents. A calm dark canvas with luminous details.",
            "Dunkle Graphittöne mit leuchtendem Mint, sanftem Orchideenlila und Apricot. Ein ruhiges Dark Theme mit strahlenden Akzenten.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexLumen",
            "A bright white canvas with iris-blue actions, soft lavender surfaces and petrol accents. Familiar, airy and quietly modern.",
            "Strahlendes Weiß mit irisblauen Akzenten, sanften Lavendelflächen und Petrol. Vertraut, luftig und dezent modern.");
        builder.AddOrUpdate("Admin.Themes.Info.FlexRiso",
            "Inspired by risograph prints: cobalt ink, fluorescent pink and sunflower yellow on bright white paper. Condensed DIN-style headings and playful off-register accents.",
            "Inspiriert vom Risographie-Druck: Kobaltblau, Neonpink und Sonnengelb auf strahlend weißem Papier. Kompakte Überschriften im DIN-Stil und verspielte, leicht versetzte Farbakzente.");

        builder.AddOrUpdate("Admin.Configuration.Themes.Option.KeepSassCompilerInMemory",
            "Keep Sass compiler in memory",
            "Sass-Compiler im Arbeitsspeicher halten",
            "Recommended while actively editing Sass files or changing theme variables in the admin area. Keeping the Dart Sass compiler running speeds up repeated compilations but uses additional memory. Otherwise, leave this disabled; an unused compiler is stopped after a short idle period.",
            "Empfohlen, wenn Sie gerade intensiv Sass-Dateien bearbeiten oder Theme-Variablen im Backend ändern. Der Dart-Sass-Compiler bleibt aktiv und beschleunigt wiederholte Kompilierungen, benötigt aber zusätzlichen Arbeitsspeicher. Ansonsten deaktiviert lassen; ein ungenutzter Compiler wird nach kurzer Zeit beendet.");

        builder.Delete(
            "Admin.Configuration.Settings.Performance.KeepSassCompilerInMemory",
            "Admin.Configuration.Settings.Performance.KeepSassCompilerInMemory.Hint");

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

        builder.AddOrUpdate("Smartstore.AI.Prompts.DontUseHtml",
            "Return plain text only. Do not use HTML tags.",
            "Gib ausschließlich Klartext zurück. Verwende keine HTML-Tags.");

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

        builder.AddOrUpdate("Admin.Common.AddEntry", "Add entry…", "Eintrag hinzufügen…");
        builder.AddOrUpdate("Admin.Common.Reorder", "Reorder (Up/Down arrow keys)", "Reihenfolge ändern (Pfeiltasten hoch/runter)");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestReasons",
            "Available return reasons",
            "Retourengründe",
            "Specify the reasons customers can select when submitting a return request. Add a separate entry for each reason.",
            "Legen Sie die Gründe fest, die Kunden bei einem Retourenantrag auswählen können. Fügen Sie für jeden Grund einen eigenen Eintrag hinzu.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestActions",
            "Available return actions",
            "Gewünschte Aktion für Retoure",
            "Specify the actions customers can select when submitting a return request. Add a separate entry for each action. These actions are not used for legal withdrawal.",
            "Legen Sie die Aktionen fest, die Kunden bei einem Retourenantrag auswählen können. Fügen Sie für jede Aktion einen eigenen Eintrag hinzu. Diese Aktionen werden nicht für den gesetzlichen Widerruf verwendet.");

        builder.AddOrUpdate("Admin.Catalog.Categories.Fields.PageSizeOptions",
            "Page size options",
            "Seitengrößen",
            "Specify the page sizes customers can select in this category's product list. Leave empty to use the page sizes from the catalog settings.",
            "Legt die auswählbaren Seitengrößen der Produktliste dieser Warengruppe fest. Lassen Sie das Feld leer, um die Seitengrößen der Katalog-Einstellungen zu verwenden.");

        builder.AddOrUpdate("Admin.Catalog.Manufacturers.Fields.PageSizeOptions",
            "Page size options",
            "Seitengrößen",
            "Specify the page sizes customers can select in this manufacturer's product list. Leave empty to use the page sizes from the catalog settings.",
            "Legt die auswählbaren Seitengrößen der Produktliste dieses Herstellers fest. Lassen Sie das Feld leer, um die Seitengrößen der Katalog-Einstellungen zu verwenden.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultPageSizeOptions",
            "Page size options",
            "Seitengrößen",
            "Specify the page sizes customers can select in product lists.",
            "Legt die auswählbaren Seitengrößen in Produktlisten fest.");

        builder.AddOrUpdate("Admin.Catalog.Products.Fields.AllowedQuantities",
            "Custom quantities",
            "Benutzerdefinierte Mengenliste",
            "Specify the order quantities customers can select for this product. If this field is populated, the minimum, maximum and step settings are ignored.",
            "Bestimmen Sie hier die Bestellmengen, die Kunden für dieses Produkt auswählen können. Wenn dieses Feld befüllt ist, werden die Einstellungen für Mindestmenge, Höchstmenge und Mengenschritt ignoriert.");

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.Salutations",
            "Salutations",
            "Anreden",
            "Specify the entries available for salutation selection when entering an address.",
            "Bestimmen Sie hier die Einträge, die bei der Adresserfassung als Anrede ausgewählt werden können.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DisplayAllImagesNumber",
            "Show all product images up to",
            "Alle Produktbilder anzeigen bis",
            "Specifies the total number of product images up to which all images are displayed. When there are more images, the gallery only shows images of the selected variant and images not assigned to any variant. The limit can be set individually for each product.",
            "Legt fest, bis zu welcher Gesamtzahl alle Produktbilder angezeigt werden. Bei mehr Bildern zeigt die Galerie nur Bilder der gewählten Variante und Bilder ohne Variantenzuordnung. Die Grenze kann für jedes Produkt individuell festgelegt werden.");

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

        builder.AddOrUpdate("Products.EmailAFriend.LoginNote",
            "Please log in to use this function. <a href=\"{0}\" rel=\"nofollow\">Login now</a>",
            "Bitte melden Sie sich an, um diese Funktion nutzen zu können. <a href=\"{0}\" rel=\"nofollow\">Jetzt anmelden</a>");

        builder.AddOrUpdate("Admin.Common.InvalidFileName", "Invalid file name \"{0}\".", "Ungültiger Dateiname \"{0}\".");

        builder.AddOrUpdate("Admin.Configuration.Settings.Performance.MaxVariantPriceCalculations",
            "Maximum number of price calculations for color and image swatches",
            "Maximale Anzahl an Preisberechnungen bei Farb- und Bildmustern",
            "Maximum number of full price calculations for color and image swatches, provided that attribute combinations with fixed prices exist. If this limit is exceeded, the prices for swatches are hidden.",
            "Maximale Anzahl vollständiger Preisberechnungen bei Farb- und Bildmustern, sofern Attributkombinationen mit festen Preisen existieren. Bei Überschreitung dieser Grenze werden die Preise für Farb- und Bildmuster ausgeblendet.");

        builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.SelectAttributes",
            "Set the attributes for the combination.",
            "Legen Sie die Attribute für die Kombination fest");

        builder.AddOrUpdate("Admin.Rules.FilterDescriptor.DiscountApplied", "Discount code applied", "Rabattcode angewendet");
        builder.AddOrUpdate("Admin.Rules.FilterDescriptor.GiftCardApplied", "Gift card applied", "Geschenkgutschein angewendet");
    }
}
