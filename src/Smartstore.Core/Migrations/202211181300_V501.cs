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
            builder.AddOrUpdate("Admin.Plugins.KnownGroup.StoreFront",
     "Store Front",
     "Front-End",
     "نمای فروشگاه");

            builder.AddOrUpdate("Admin.Configuration.Settings.Tax.EuVatEnabled.Hint")
                .Value("de", "Legt die EU-Konforme MwSt.-Berechnung fest.")
                .Value("fa", "محاسبه مالیات بر ارزش افزوده مطابق با استانداردهای اتحادیه اروپا را تنظیم می‌کند.");

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
       "قیمت مقایسه",
       "Sets a comparison price, e.g.: MSRP, list price, regular price before discount, etc. The comparison price serves as the strike price.",
       "Legt einen Vergleichspreis fest, z.B.: UVP, Listenpreis, regulärer Preis vor einer Ermäßigung etc. Der Vergleichspreis dient als Streichpreis.",
       "قیمت مقایسه را تنظیم می‌کند، مثلاً: قیمت پیشنهادی خرده‌فروشی، قیمت لیست، قیمت عادی قبل از تخفیف و غیره. قیمت مقایسه به عنوان قیمت خط‌خورده عمل می‌کند.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.Fields.IsVerfifiedPurchase",
                "Is verified",
                "Ist verifiziert",
                "تأییدشده است",
                "Specifies whether this product review was written by a customer who purchased the product from this store.",
                "Legt fest, ob diese Produktbewertung von einem Kunden verfasst wurde, der das Produkt in diesem Shop gekauft hat.",
                "مشخص می‌کند که آیا این نظر محصول توسط مشتری‌ای نوشته شده که محصول را از این فروشگاه خریداری کرده است.");

            builder.AddOrUpdate("Reviews.Verified",
                "Verified purchase",
                "Verifizierter Kauf",
                "خرید تأییدشده");

            builder.AddOrUpdate("Reviews.Unverified",
                "Unverified purchase",
                "Nicht verifiziert",
                "خرید تأییدنشده");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberVerfifiedReviews",
                "There were {0} product reviews verified.",
                "Es wurden {0} Produktrezensionen verifiziert.",
                "{0} نظر محصول تأیید شدند.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberApprovedReviews",
                "There were {0} product reviews approved.",
                "Es wurden {0} Produktrezensionen genehmigt.",
                "{0} نظر محصول تأیید شدند.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberDisapprovedReviews",
                "There were {0} product reviews disapproved.",
                "Es wurden {0} Produktrezensionen abgelehnt.",
                "{0} نظر محصول رد شدند.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.VerifySelected",
                "Verify selected",
                "Ausgewählte verfizieren",
                "تأیید موارد انتخاب‌شده");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowManufacturerInProductDetail",
                "Display manufacturer",
                "Hersteller anzeigen",
                "نمایش تولیدکننده");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowVerfiedPurchaseBadge",
                "Show verified purchase badge",
                "Zeige Badge für verifizierte Käufe",
                "نمایش نشان خرید تأییدشده",
                "Displays a badge on product reviews to indicate whether the writer is a verified buyer.",
                "Zeigt bei Produktrezensionen einen Badge an, der anzeigt, ob der Verfasser ein verifizierter Käufer ist.",
                "یک نشان روی نظرات محصول نمایش می‌دهد که مشخص می‌کند آیا نویسنده یک خریدار تأییدشده است.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.PleaseSelect",
                "Please select the attribute that should be added.",
                "Bitte wählen Sie das Attribut, dass hinzugefügt werden soll.",
                "لطفاً ویژگی‌ای که باید اضافه شود را انتخاب کنید.");

            builder.AddOrUpdate("Admin.Theme.GoogleFonts.Gdpr.Hint",
                "Please note that the use of Google Fonts without local upload violates the EU GDPR according to a ruling of the LG Munich (20.01.2022, Az. 3 O 17493/20). Please inform yourself about the current legal situation before using web fonts. Smartstore is not liable for any possible consequences.",
                "Bitte beachten Sie, dass der Einsatz von Google Fonts ohne lokale Einbindung laut Urteil vom LG München (20.01.2022, Az. 3 O 17493/20) gegen die DSGVO verstößt. Bitte informieren Sie sich über die aktuelle Rechtslage, bevor Sie Web Fonts einsetzen. Smartstore übernimmt keinerlei Haftung.",
                "لطفاً توجه کنید که استفاده از فونت‌های گوگل بدون بارگذاری محلی طبق رأی دادگاه مونیخ (20.01.2022، شماره 3 O 17493/20) نقض GDPR اتحادیه اروپا است. لطفاً قبل از استفاده از فونت‌های وب، از وضعیت قانونی فعلی اطلاع پیدا کنید. Smartstore هیچ مسئولیتی در قبال عواقب احتمالی ندارد.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.SetDefaultComparePriceLabel",
                "Make default for compare price",
                "Ist Standard für Vergleichspreis",
                "به عنوان پیش‌فرض برای قیمت مقایسه تنظیم کن");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.SetDefaultRegularPriceLabel",
                "Make default for regular price",
                "Ist Standard für regulären Streichpreis",
                "به عنوان پیش‌فرض برای قیمت عادی تنظیم کن");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.IsDefaultComparePriceLabel",
                "Is compare price default",
                "Ist Standard für Vergleichspreis",
                "پیش‌فرض قیمت مقایسه است");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.IsDefaultRegularPriceLabel",
                "Is regular price default",
                "Ist Standard für regulärer Preis",
                "پیش‌فرض قیمت عادی است");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.CantDeleteDefaultComparePriceLabel",
                "Cannot delete default label for compare price.",
                "Das Standard Label für den Vergleichspreis kann nicht gelöscht werden.",
                "نمی‌توان برچسب پیش‌فرض برای قیمت مقایسه را حذف کرد.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.CantDeleteDefaultRegularPriceLabel",
                "Cannot delete default label for regular price.",
                "Das Standard Label für den regulären Preis kann nicht gelöscht werden.",
                "نمی‌توان برچسب پیش‌فرض برای قیمت عادی را حذف کرد.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowOfferBadgeInLists",
                "Show offer badge in lists",
                "Angebots-Badge in Listen anzeigen",
                "نمایش نشان پیشنهاد در لیست‌ها",
                "Specifies whether to display offer badge in product lists.",
                "Bestimmt, ob Angebots-Badges in Produkt-Listen angezeigt werden.",
                "مشخص می‌کند که آیا نشان پیشنهاد در لیست محصولات نمایش داده شود.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowPriceLabelInLists",
                "Show price label in lists",
                "Zeige Preis-Label in Listen",
                "نمایش برچسب قیمت در لیست‌ها",
                "Specifies whether the label of the compare price is displayed in product lists.",
                "Bestimmt, ob das Label des Vergleichspreises in Produkt-Listen angezeigt wird.",
                "مشخص می‌کند که آیا برچسب قیمت مقایسه در لیست محصولات نمایش داده شود.");

            builder.AddOrUpdate("Products.InclTaxSuffix",
                "{0} *",
                "{0} *",
                "{0} *");

            builder.AddOrUpdate("Products.ExclTaxSuffix",
                "{0} *",
                "{0} *",
                "{0} *");

            builder.AddOrUpdate("ShoppingCart.OutOfStock")
                .Value("de", "Ausverkauft")
                .Value("fa", "ناموجود");

            builder.AddOrUpdate("Products.Availability.InStockWithQuantity")
                .Value("de", "{0} auf Lager")
                .Value("fa", "{0} در انبار");

            builder.AddOrUpdate("Products.Availability.Backordering")
                .Value("de", "Ausverkauft - wird nachgeliefert, sobald wieder auf Lager.")
                .Value("fa", "ناموجود - به محض موجود شدن ارسال خواهد شد.");
        }
    }
}
