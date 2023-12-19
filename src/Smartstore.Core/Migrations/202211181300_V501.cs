using FluentMigrator;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-11-18 13:00:00", "V501")]
    internal class V501 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
        }

        public override void Down()
        {
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Plugins.KnownGroup.StoreFront", "Store Front", "Front-End");

            builder.AddOrUpdate("Admin.Configuration.Settings.Tax.EuVatEnabled.Hint")
                .Value("de", "Legt die EU-Konforme MwSt.-Berechnung fest.");

            builder.Delete(
                "Admin.System.Log.BackToList",
                "Admin.Promotions.Campaigns.BackToList",
                "Admin.Orders.BackToList",
                "Admin.Customers.Customers.BackToList",
                "Admin.Customers.CustomerRoles.BackToList",
                "Admin.ContentManagement.Polls.BackToList",
                "Admin.ContentManagement.MessageTemplates.BackToList",
                "Admin.Configuration.Tax.Providers.BackToList",
                "Admin.Configuration.SMSProviders.BackToList",
                "Admin.Configuration.Shipping.Providers.BackToList",
                "Admin.Configuration.Shipping.Methods.BackToList",
                "Admin.Configuration.Plugins.Misc.BackToList",
                "Admin.Configuration.Payment.Methods.BackToList",
                "Admin.Configuration.ExternalAuthenticationMethods.BackToList",
                "Admin.Configuration.DeliveryTimes.BackToList",
                "Admin.Configuration.Countries.BackToList",
                "Admin.Catalog.Products.BackToList",
                "Admin.Catalog.Attributes.CheckoutAttributes.BackToList",
                "Admin.Affiliates.BackToList");

            builder.Delete(
                "Admin.Catalog.BulkEdit",
                "Admin.Catalog.BulkEdit.Fields.ManageInventoryMethod",
                "Admin.Catalog.BulkEdit.Fields.Name",
                "Admin.Catalog.BulkEdit.Fields.OldPrice",
                "Admin.Catalog.BulkEdit.Fields.Price",
                "Admin.Catalog.BulkEdit.Fields.Published",
                "Admin.Catalog.BulkEdit.Fields.SKU",
                "Admin.Catalog.BulkEdit.Fields.StockQuantity",
                "Admin.Catalog.BulkEdit.Info",
                "Admin.Catalog.BulkEdit.List.SearchCategory",
                "Admin.Catalog.BulkEdit.List.SearchCategory.Hint",
                "Admin.Catalog.BulkEdit.List.SearchManufacturer",
                "Admin.Catalog.BulkEdit.List.SearchManufacturer.Hint",
                "Admin.Catalog.BulkEdit.List.SearchProductName",
                "Admin.Catalog.BulkEdit.List.SearchProductName.Hint");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.ComparePrice",
                "Compare price",
                "Vergleichspreis",
                "Sets a comparison price, e.g.: MSRP, list price, regular price before discount, etc. The comparison price serves as the strike price.",
                "Legt einen Vergleichspreis fest, z.B.: UVP, Listenpreis, regulärer Preis vor einer Ermäßigung etc. Der Vergleichspreis dienst als Streichpreis.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.Fields.IsVerfifiedPurchase",
                "Is verified",
                "Ist verifiziert",
                "Specifies whether this product review was written by a customer who purchased the product from this store.",
                "Legt fest, ob diese Produktbewertung von einem Kunden verfasst wurde, der das Produkt in diesem Shop gekauft hat.");

            builder.AddOrUpdate("Reviews.Verified", "Verified purchase", "Verifizierter Kauf");
            builder.AddOrUpdate("Reviews.Unverified", "Unverified purchase", "Nicht verifiziert");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberVerfifiedReviews",
                "There were {0} product reviews verfified.",
                "Es wurden {0} Produktrezensionen verifiziert.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberApprovedReviews",
                "There were {0} product reviews approved.",
                "Es wurden {0} Produktrezensionen genehmigt.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberDisapprovedReviews",
                "There were {0} product reviews disapproved.",
                "Es wurden {0} Produktrezensionen abgelehnt.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.VerifySelected",
                "Verify selected",
                "Ausgewählte verfizieren");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerInProductDetail", "Display manufacturer", "Hersteller anzeigen");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowVerfiedPurchaseBadge",
                "Show verfied purchase badge",
                "Zeige Badge für verifizierte Käufe",
                "Displays a badge on product reviews to indicate whether the writer is a verified buyer.",
                "Zeigt bei Produktrezensionen einen Badge an, der anzeigt, ob der Verfasser ein verifizierter Käufer ist.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.PleaseSelect",
                "Please select the attribute that should be added.",
                "Bitte wählen Sie das Attribut, dass hinzugefügt werden soll.");

            builder.AddOrUpdate("Admin.Theme.GoogleFonts.Gdpr.Hint",
                "Please note that the use of Google Fonts without local upload violates the EU GDPR according to a ruling of the LG Munich (20.01.2022, Az. 3 O 17493/20). Please inform yourself about the current legal situation before using web fonts. Smartstore is not liable for any possible consequences.",
                "Bitte beachten Sie, dass der Einsatz von Google Fonts ohne lokale Einbindung laut Urteil vom LG München (20.01.2022, Az. 3 O 17493/20) gegen die DSGVO verstößt. Bitte informieren Sie sich über die aktuelle Rechtslage, bevor Sie Web Fonts einsetzen. Smartstore übernimmt keinerlei Haftung.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.SetDefaultComparePriceLabel",
                "Make default for compare price",
                "Ist Standard für Vergleichspreis");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.SetDefaultRegularPriceLabel",
                "Make default for regular price",
                "Ist Standard für regulären Streichpreis");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.IsDefaultComparePriceLabel",
                "Is compare price default",
                "Ist Standard für Vergleichspreis");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.IsDefaultRegularPriceLabel",
                "Is regular price default",
                "Ist Standard für regulärer Preis");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.CantDeleteDefaultComparePriceLabel",
                "Cannot delete default label for compare price.",
                "Das Standard Label für den Vergleichspreis kann nicht gelöscht werden.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.CantDeleteDefaultRegularPriceLabel",
                "Cannot delete default label for regular price.",
                "Das Standard Label für den regulären Preis kann nicht gelöscht werden.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowOfferBadgeInLists",
                "Show offer badge in lists",
                "Angebots-Badge in Listen anzeigen",
                "Specifies whether to display offer badge in product lists.",
                "Bestimmt, ob Angebots-Badges in Produkt-Listen angezeigt werden.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowPriceLabelInLists",
                "Show price label in lists",
                "Zeige Preis-Label in Listen",
                "Specifies whether the label of the compare price is displayed in product lists.",
                "Bestimmt, ob das Label des Vergleichspreises in Produkt-Listen angezeigt wird.");

            builder.AddOrUpdate("Products.InclTaxSuffix", "{0} *", "{0} *");
            builder.AddOrUpdate("Products.ExclTaxSuffix", "{0} *", "{0} *");

            builder.AddOrUpdate("ShoppingCart.OutOfStock").Value("de", "Ausverkauft");
            builder.AddOrUpdate("Products.Availability.InStockWithQuantity").Value("de", "{0} auf Lager");
            builder.AddOrUpdate("Products.Availability.Backordering").Value("de", "Ausverkauft - wird nachgeliefert, sobald wieder auf Lager.");
        }
    }
}
