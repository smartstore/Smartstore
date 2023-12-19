using FluentMigrator;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-03-30 11:00:00", "V504")]
    internal class V504 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            builder.AddOrUpdate("Admin.System.SystemInfo.DataProviderFriendlyName", "Database", "Datenbank");
            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountLimitationsInLists",
                "Validate discount limitations in product lists",
                "Prüfe die Rabattgültigkeit in Produktlisten",
                "Enabling this option may reduce the performance.",
                "Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountGiftCardsInLists",
                "Check cart for gift cards when validating discounts in product lists",
                "Prüfe den Warenkorb auf Gutscheine bei der Rabattvalidierung in Produktlisten",
                "Specifies whether to check the shopping cart for the existence of gift cards when validating discounts in product lists. In case of gift cards no discount is applied because the customer could earn money through that. Enabling this option may reduce the performance.",
                "Legt fest, ob bei der Rabattvalidierung in Produktlisten der Warenkorb auf vorhandene Gutscheine überprüft werden soll. Bei Gutscheinen wird kein Rabatt gewährt, weil der Kunde damit Geld verdienen könnte. Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountRulesInLists",
                "Validate cart rules of discounts in product lists",
                "Prüfe Warenkorbregeln von Rabatten in Produktlisten",
                "Enabling this option may reduce the performance.",
                "Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Common.Entity.SelectProducts", "Select products", "Produkte auswählen");
            builder.AddOrUpdate("Common.Entity.SelectCategories", "Select categories", "Warengruppen auswählen");
            builder.AddOrUpdate("Common.Entity.SelectManufacturers", "Select manufacturers", "Hersteller auswählen");
            builder.AddOrUpdate("Common.Entity.SelectTopics", "Select topics", "Seiten auswählen");

            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo", "Table statistics", "Tabellenstatistik");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.TableName", "Table name", "Tabelle");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.NumRows", "Rows", "Datensätze");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.TotalSpace", "Total space", "Gesamtgröße");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.UsedSpace", "Used space", "Genutzt");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.UnusedSpace", "Unused", "Ungenutzt");

            builder.AddOrUpdate("Admin.Configuration.Settings.Media.OffloadEmbeddedImagesOnSave",
                "Offload embedded Base64 images on save",
                "Eingebettete Base64-Bilder beim Speichern extrahieren",
                "Finds embedded Base64 images in long HTML descriptions, extracts and saves them to the media storage, and replaces the Base64 fragment with the media path. Offloading is automatically triggered by saving an entity to the database. Currently supported entity types are: Product, Category, Manufacturer and Topic.",
                "Findet eingebettete Base64-Bilder in langen HTML-Beschreibungen, extrahiert und speichert sie im Medienspeicher und ersetzt das Base64-Fragment durch den Medienpfad. Die Extraktion wird automatisch ausgelöst, wenn eine Entität in der Datenbank gespeichert wird. Derzeit unterstützte Entitätstypen sind: Produkt, Warengruppe, Hersteller und Seite.");

            builder.AddOrUpdate("Admin.Configuration.Plugins.Description.Step1",
                "Use the <a id='{0}' href='{1}' data-toggle='modal'>package uploader</a> or upload the plugin manually - eg. via FTP - to the <i>/Modules</i> folder in your Smartstore directory.",
                "Verwenden Sie den <a id='{0}' href='{1}' data-toggle='modal'>Paket Uploader</a> oder laden Sie das Plugin manuell - bspw. per FTP - in den <i>/Modules</i> Ordner hoch.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Search.SearchFieldsNote",
                "The Name, SKU and Short Description fields can be searched in the standard search. Other fields require a search plugin such as the MegaSearch plugin from <a href='https://smartstore.com/en/editions-prices' target='_blank'>Premium Edition</a>.",
                "In der Standardsuche können die Felder Name, SKU und Kurzbeschreibung durchsucht werden. Für weitere Felder ist ein Such-Plugin wie etwa das MegaSearch-Plugin aus der <a href='https://smartstore.com/en/editions-prices' target='_blank'>Premium Edition</a> notwendig.");

            #region Remove old enum resources 

            var prefix = "Enums.SmartStore.Core.Domain.";
            builder.Delete($"{prefix}Blogs.PreviewDisplayType.Bare");
            builder.Delete($"{prefix}Blogs.PreviewDisplayType.Default");
            builder.Delete($"{prefix}Blogs.PreviewDisplayType.DefaultSectionBg");
            builder.Delete($"{prefix}Blogs.PreviewDisplayType.Preview");
            builder.Delete($"{prefix}Blogs.PreviewDisplayType.PreviewSectionBg");
            builder.Delete($"{prefix}Blogs.PreviewDisplayType.SectionBg");
            builder.Delete($"{prefix}Catalog.AttributeChoiceBehaviour.GrayOutUnavailable");
            builder.Delete($"{prefix}Catalog.AttributeChoiceBehaviour.None");
            builder.Delete($"{prefix}Catalog.AttributeControlType.Boxes");
            builder.Delete($"{prefix}Catalog.AttributeControlType.Checkboxes");
            builder.Delete($"{prefix}Catalog.AttributeControlType.Datepicker");
            builder.Delete($"{prefix}Catalog.AttributeControlType.DropdownList");
            builder.Delete($"{prefix}Catalog.AttributeControlType.FileUpload");
            builder.Delete($"{prefix}Catalog.AttributeControlType.MultilineTextbox");
            builder.Delete($"{prefix}Catalog.AttributeControlType.RadioList");
            builder.Delete($"{prefix}Catalog.AttributeControlType.TextBox");
            builder.Delete($"{prefix}Catalog.BackorderMode.AllowQtyBelow0");
            builder.Delete($"{prefix}Catalog.BackorderMode.AllowQtyBelow0AndNotifyCustomer");
            builder.Delete($"{prefix}Catalog.BackorderMode.NoBackorders");
            builder.Delete($"{prefix}Catalog.DownloadActivationType.Manually");
            builder.Delete($"{prefix}Catalog.DownloadActivationType.WhenOrderIsPaid");
            builder.Delete($"{prefix}Catalog.GiftCardType.Physical");
            builder.Delete($"{prefix}Catalog.GiftCardType.Virtual");
            builder.Delete($"{prefix}Catalog.GridColumnSpan.Max2Cols");
            builder.Delete($"{prefix}Catalog.GridColumnSpan.Max3Cols");
            builder.Delete($"{prefix}Catalog.GridColumnSpan.Max4Cols");
            builder.Delete($"{prefix}Catalog.GridColumnSpan.Max5Cols");
            builder.Delete($"{prefix}Catalog.GridColumnSpan.Max6Cols");
            builder.Delete($"{prefix}Catalog.LowStockActivity.DisableBuyButton");
            builder.Delete($"{prefix}Catalog.LowStockActivity.Nothing");
            builder.Delete($"{prefix}Catalog.LowStockActivity.Unpublish");
            builder.Delete($"{prefix}Catalog.ManageInventoryMethod.DontManageStock");
            builder.Delete($"{prefix}Catalog.ManageInventoryMethod.ManageStock");
            builder.Delete($"{prefix}Catalog.ManageInventoryMethod.ManageStockByAttributes");
            builder.Delete($"{prefix}Catalog.PriceDisplayStyle.BadgeAll");
            builder.Delete($"{prefix}Catalog.PriceDisplayStyle.BadgeFreeProductsOnly");
            builder.Delete($"{prefix}Catalog.PriceDisplayStyle.Default");
            builder.Delete($"{prefix}Catalog.PriceDisplayType.Hide");
            builder.Delete($"{prefix}Catalog.PriceDisplayType.LowestPrice");
            builder.Delete($"{prefix}Catalog.PriceDisplayType.PreSelectedPrice");
            builder.Delete($"{prefix}Catalog.PriceDisplayType.PriceWithoutDiscountsAndAttributes");
            builder.Delete($"{prefix}Catalog.ProductCondition.Damaged");
            builder.Delete($"{prefix}Catalog.ProductCondition.New");
            builder.Delete($"{prefix}Catalog.ProductCondition.Refurbished");
            builder.Delete($"{prefix}Catalog.ProductCondition.Used");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.CreatedOn");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.CreatedOnAsc");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.Initial");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.NameAsc");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.NameDesc");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.PriceAsc");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.PriceDesc");
            builder.Delete($"{prefix}Catalog.ProductSortingEnum.Relevance");
            builder.Delete($"{prefix}Catalog.ProductType.BundledProduct");
            builder.Delete($"{prefix}Catalog.ProductType.GroupedProduct");
            builder.Delete($"{prefix}Catalog.ProductType.SimpleProduct");
            builder.Delete($"{prefix}Catalog.ProductVariantAttributeValueType.ProductLinkage");
            builder.Delete($"{prefix}Catalog.ProductVariantAttributeValueType.Simple");
            builder.Delete($"{prefix}Catalog.ProductVisibility.Full");
            builder.Delete($"{prefix}Catalog.ProductVisibility.Hidden");
            builder.Delete($"{prefix}Catalog.ProductVisibility.ProductPage");
            builder.Delete($"{prefix}Catalog.ProductVisibility.SearchResults");
            builder.Delete($"{prefix}Catalog.RecurringProductCyclePeriod.Days");
            builder.Delete($"{prefix}Catalog.RecurringProductCyclePeriod.Months");
            builder.Delete($"{prefix}Catalog.RecurringProductCyclePeriod.Weeks");
            builder.Delete($"{prefix}Catalog.RecurringProductCyclePeriod.Years");
            builder.Delete($"{prefix}Catalog.SubCategoryDisplayType.AboveProductList");
            builder.Delete($"{prefix}Catalog.SubCategoryDisplayType.Bottom");
            builder.Delete($"{prefix}Catalog.SubCategoryDisplayType.Hide");
            builder.Delete($"{prefix}Common.PageTitleSeoAdjustment.PagenameAfterStorename");
            builder.Delete($"{prefix}Common.PageTitleSeoAdjustment.StorenameAfterPagename");
            builder.Delete($"{prefix}Customers.CustomerLoginType.Email");
            builder.Delete($"{prefix}Customers.CustomerLoginType.Username");
            builder.Delete($"{prefix}Customers.CustomerLoginType.UsernameOrEmail");
            builder.Delete($"{prefix}Customers.CustomerNameFormat.ShowEmails");
            builder.Delete($"{prefix}Customers.CustomerNameFormat.ShowFirstName");
            builder.Delete($"{prefix}Customers.CustomerNameFormat.ShowFullNames");
            builder.Delete($"{prefix}Customers.CustomerNameFormat.ShowNameAndCity");
            builder.Delete($"{prefix}Customers.CustomerNameFormat.ShowUsernames");
            builder.Delete($"{prefix}Customers.CustomerNumberMethod.AutomaticallySet");
            builder.Delete($"{prefix}Customers.CustomerNumberMethod.Disabled");
            builder.Delete($"{prefix}Customers.CustomerNumberMethod.Enabled");
            builder.Delete($"{prefix}Customers.CustomerNumberVisibility.Display");
            builder.Delete($"{prefix}Customers.CustomerNumberVisibility.Editable");
            builder.Delete($"{prefix}Customers.CustomerNumberVisibility.EditableIfEmpty");
            builder.Delete($"{prefix}Customers.CustomerNumberVisibility.None");
            builder.Delete($"{prefix}Customers.PasswordFormat.Clear");
            builder.Delete($"{prefix}Customers.PasswordFormat.Encrypted");
            builder.Delete($"{prefix}Customers.PasswordFormat.Hashed");
            builder.Delete($"{prefix}Customers.WalletPostingReason.Admin");
            builder.Delete($"{prefix}Customers.WalletPostingReason.Debit");
            builder.Delete($"{prefix}Customers.WalletPostingReason.PartialRefund");
            builder.Delete($"{prefix}Customers.WalletPostingReason.Purchase");
            builder.Delete($"{prefix}Customers.WalletPostingReason.Refill");
            builder.Delete($"{prefix}Customers.WalletPostingReason.Refund");
            builder.Delete($"{prefix}DataExchange.ExportAttributeValueMerging.AppendAllValuesToName");
            builder.Delete($"{prefix}DataExchange.ExportAttributeValueMerging.None");
            builder.Delete($"{prefix}DataExchange.ExportDeploymentType.Email");
            builder.Delete($"{prefix}DataExchange.ExportDeploymentType.FileSystem");
            builder.Delete($"{prefix}DataExchange.ExportDeploymentType.Ftp");
            builder.Delete($"{prefix}DataExchange.ExportDeploymentType.Http");
            builder.Delete($"{prefix}DataExchange.ExportDeploymentType.PublicFolder");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.Description");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.ManufacturerAndNameAndDescription");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.ManufacturerAndNameAndShortDescription");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.NameAndDescription");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.NameAndShortDescription");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.None");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.ShortDescription");
            builder.Delete($"{prefix}DataExchange.ExportDescriptionMerging.ShortDescriptionOrNameIfEmpty");
            builder.Delete($"{prefix}DataExchange.ExportEntityType.Category");
            builder.Delete($"{prefix}DataExchange.ExportEntityType.Customer");
            builder.Delete($"{prefix}DataExchange.ExportEntityType.Manufacturer");
            builder.Delete($"{prefix}DataExchange.ExportEntityType.NewsLetterSubscription");
            builder.Delete($"{prefix}DataExchange.ExportEntityType.Order");
            builder.Delete($"{prefix}DataExchange.ExportEntityType.Product");
            builder.Delete($"{prefix}DataExchange.ExportEntityType.ShoppingCartItem");
            builder.Delete($"{prefix}DataExchange.ExportHttpTransmissionType.MultipartFormDataPost");
            builder.Delete($"{prefix}DataExchange.ExportHttpTransmissionType.SimplePost");
            builder.Delete($"{prefix}DataExchange.ExportOrderStatusChange.Complete");
            builder.Delete($"{prefix}DataExchange.ExportOrderStatusChange.None");
            builder.Delete($"{prefix}DataExchange.ExportOrderStatusChange.Processing");
            builder.Delete($"{prefix}DataExchange.ImportEntityType.Category");
            builder.Delete($"{prefix}DataExchange.ImportEntityType.Customer");
            builder.Delete($"{prefix}DataExchange.ImportEntityType.NewsLetterSubscription");
            builder.Delete($"{prefix}DataExchange.ImportEntityType.Product");
            builder.Delete($"{prefix}DataExchange.ImportFileType.CSV");
            builder.Delete($"{prefix}DataExchange.ImportFileType.XLSX");
            builder.Delete($"{prefix}DataExchange.RelatedEntityType.ProductVariantAttributeCombination");
            builder.Delete($"{prefix}DataExchange.RelatedEntityType.ProductVariantAttributeValue");
            builder.Delete($"{prefix}DataExchange.RelatedEntityType.TierPrice");
            builder.Delete($"{prefix}Directory.CurrencyRoundingRule.AlwaysRoundDown");
            builder.Delete($"{prefix}Directory.CurrencyRoundingRule.AlwaysRoundUp");
            builder.Delete($"{prefix}Directory.CurrencyRoundingRule.RoundMidpointDown");
            builder.Delete($"{prefix}Directory.CurrencyRoundingRule.RoundMidpointUp");
            builder.Delete($"{prefix}Directory.DeliveryTimesPresentation.DateOnly");
            builder.Delete($"{prefix}Directory.DeliveryTimesPresentation.LabelAndDate");
            builder.Delete($"{prefix}Directory.DeliveryTimesPresentation.LabelOnly");
            builder.Delete($"{prefix}Directory.DeliveryTimesPresentation.None");
            builder.Delete($"{prefix}Discounts.DiscountLimitationType.NTimesOnly");
            builder.Delete($"{prefix}Discounts.DiscountLimitationType.NTimesPerCustomer");
            builder.Delete($"{prefix}Discounts.DiscountLimitationType.Unlimited");
            builder.Delete($"{prefix}Discounts.DiscountType.AssignedToCategories");
            builder.Delete($"{prefix}Discounts.DiscountType.AssignedToManufacturers");
            builder.Delete($"{prefix}Discounts.DiscountType.AssignedToOrderSubTotal");
            builder.Delete($"{prefix}Discounts.DiscountType.AssignedToOrderTotal");
            builder.Delete($"{prefix}Discounts.DiscountType.AssignedToShipping");
            builder.Delete($"{prefix}Discounts.DiscountType.AssignedToSkus");
            builder.Delete($"{prefix}Localization.DefaultLanguageRedirectBehaviour.DoNoRedirect");
            builder.Delete($"{prefix}Localization.DefaultLanguageRedirectBehaviour.PrependSeoCodeAndRedirect");
            builder.Delete($"{prefix}Localization.DefaultLanguageRedirectBehaviour.StripSeoCode");
            builder.Delete($"{prefix}Localization.InvalidLanguageRedirectBehaviour.FallbackToWorkingLanguage");
            builder.Delete($"{prefix}Localization.InvalidLanguageRedirectBehaviour.ReturnHttp404");
            builder.Delete($"{prefix}Localization.InvalidLanguageRedirectBehaviour.Tolerate");
            builder.Delete($"{prefix}Logging.LogLevel.Debug");
            builder.Delete($"{prefix}Logging.LogLevel.Error");
            builder.Delete($"{prefix}Logging.LogLevel.Fatal");
            builder.Delete($"{prefix}Logging.LogLevel.Information");
            builder.Delete($"{prefix}Logging.LogLevel.Warning");
            builder.Delete($"{prefix}Orders.CheckoutNewsLetterSubscription.Activated");
            builder.Delete($"{prefix}Orders.CheckoutNewsLetterSubscription.Deactivated");
            builder.Delete($"{prefix}Orders.CheckoutNewsLetterSubscription.None");
            builder.Delete($"{prefix}Orders.CheckoutThirdPartyEmailHandOver.Activated");
            builder.Delete($"{prefix}Orders.CheckoutThirdPartyEmailHandOver.Deactivated");
            builder.Delete($"{prefix}Orders.CheckoutThirdPartyEmailHandOver.None");
            builder.Delete($"{prefix}Orders.OrderStatus.Cancelled");
            builder.Delete($"{prefix}Orders.OrderStatus.Complete");
            builder.Delete($"{prefix}Orders.OrderStatus.Pending");
            builder.Delete($"{prefix}Orders.OrderStatus.Processing");
            builder.Delete($"{prefix}Orders.ReturnRequestStatus.Cancelled");
            builder.Delete($"{prefix}Orders.ReturnRequestStatus.ItemsRefunded");
            builder.Delete($"{prefix}Orders.ReturnRequestStatus.ItemsRepaired");
            builder.Delete($"{prefix}Orders.ReturnRequestStatus.Pending");
            builder.Delete($"{prefix}Orders.ReturnRequestStatus.Received");
            builder.Delete($"{prefix}Orders.ReturnRequestStatus.RequestRejected");
            builder.Delete($"{prefix}Orders.ReturnRequestStatus.ReturnAuthorized");
            builder.Delete($"{prefix}Orders.ShoppingCartType.ShoppingCart");
            builder.Delete($"{prefix}Orders.ShoppingCartType.Wishlist");
            builder.Delete($"{prefix}Payments.CapturePaymentReason.OrderDelivered");
            builder.Delete($"{prefix}Payments.CapturePaymentReason.OrderShipped");
            builder.Delete($"{prefix}Payments.PaymentStatus.Authorized");
            builder.Delete($"{prefix}Payments.PaymentStatus.Paid");
            builder.Delete($"{prefix}Payments.PaymentStatus.PartiallyRefunded");
            builder.Delete($"{prefix}Payments.PaymentStatus.Pending");
            builder.Delete($"{prefix}Payments.PaymentStatus.Refunded");
            builder.Delete($"{prefix}Payments.PaymentStatus.Voided");
            builder.Delete($"{prefix}Security.PasswordFormat.Clear");
            builder.Delete($"{prefix}Security.PasswordFormat.Encrypted");
            builder.Delete($"{prefix}Security.PasswordFormat.Hashed");
            builder.Delete($"{prefix}Security.UserRegistrationType.AdminApproval");
            builder.Delete($"{prefix}Security.UserRegistrationType.Disabled");
            builder.Delete($"{prefix}Security.UserRegistrationType.EmailValidation");
            builder.Delete($"{prefix}Security.UserRegistrationType.Standard");
            builder.Delete($"{prefix}Seo.CanonicalHostNameRule.NoRule");
            builder.Delete($"{prefix}Seo.CanonicalHostNameRule.OmitWww");
            builder.Delete($"{prefix}Seo.CanonicalHostNameRule.RequireWww");
            builder.Delete($"{prefix}Shipping.ShippingStatus.Delivered");
            builder.Delete($"{prefix}Shipping.ShippingStatus.NotYetShipped");
            builder.Delete($"{prefix}Shipping.ShippingStatus.PartiallyShipped");
            builder.Delete($"{prefix}Shipping.ShippingStatus.Shipped");
            builder.Delete($"{prefix}Shipping.ShippingStatus.ShippingNotRequired");
            builder.Delete($"{prefix}Tax.AuxiliaryServicesTaxType.HighestCartAmount");
            builder.Delete($"{prefix}Tax.AuxiliaryServicesTaxType.HighestTaxRate");
            builder.Delete($"{prefix}Tax.AuxiliaryServicesTaxType.ProRata");
            builder.Delete($"{prefix}Tax.AuxiliaryServicesTaxType.SpecifiedTaxCategory");
            builder.Delete($"{prefix}Tax.TaxBasedOn.BillingAddress");
            builder.Delete($"{prefix}Tax.TaxBasedOn.DefaultAddress");
            builder.Delete($"{prefix}Tax.TaxBasedOn.ShippingAddress");
            builder.Delete($"{prefix}Tax.TaxDisplayType.ExcludingTax");
            builder.Delete($"{prefix}Tax.TaxDisplayType.IncludingTax");
            builder.Delete($"{prefix}Tax.VatNumberStatus.Empty");
            builder.Delete($"{prefix}Tax.VatNumberStatus.Invalid");
            builder.Delete($"{prefix}Tax.VatNumberStatus.Unknown");
            builder.Delete($"{prefix}Tax.VatNumberStatus.Valid");

            prefix = "Enums.SmartStore.";
            builder.Delete($"{prefix}Core.Search.Facets.FacetSorting.DisplayOrder");
            builder.Delete($"{prefix}Core.Search.Facets.FacetSorting.HitsDesc");
            builder.Delete($"{prefix}Core.Search.Facets.FacetSorting.LabelAsc");
            builder.Delete($"{prefix}Core.Search.Facets.FacetSorting.ValueAsc");
            builder.Delete($"{prefix}Core.Search.Facets.FacetTemplateHint.Checkboxes");
            builder.Delete($"{prefix}Core.Search.Facets.FacetTemplateHint.Custom");
            builder.Delete($"{prefix}Core.Search.Facets.FacetTemplateHint.NumericRange");
            builder.Delete($"{prefix}Core.Search.IndexingStatus.Idle");
            builder.Delete($"{prefix}Core.Search.IndexingStatus.Rebuilding");
            builder.Delete($"{prefix}Core.Search.IndexingStatus.Unavailable");
            builder.Delete($"{prefix}Core.Search.IndexingStatus.Updating");
            builder.Delete($"{prefix}Core.Search.SearchMode.Contains");
            builder.Delete($"{prefix}Core.Search.SearchMode.ExactMatch");
            builder.Delete($"{prefix}Core.Search.SearchMode.StartsWith");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.DropoffType.BusinessServiceCenter");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.DropoffType.DropBox");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.DropoffType.RegularPickup");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.DropoffType.RequestCourier");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.DropoffType.Station");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.PackingType.PackByDimensions");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.PackingType.PackByOneItemPerPackage");
            builder.Delete($"{prefix}Plugin.Shipping.Fedex.PackingType.PackByVolume");
            builder.Delete($"{prefix}Rules.RuleScope.Cart");
            builder.Delete($"{prefix}Rules.RuleScope.Cart.Hint");
            builder.Delete($"{prefix}Rules.RuleScope.Customer");
            builder.Delete($"{prefix}Rules.RuleScope.Customer.Hint");
            builder.Delete($"{prefix}Rules.RuleScope.OrderItem");
            builder.Delete($"{prefix}Rules.RuleScope.Product");
            builder.Delete($"{prefix}Rules.RuleScope.Product.Hint");
            builder.Delete($"{prefix}Services.Payments.RecurringPaymentType.Automatic");
            builder.Delete($"{prefix}Services.Payments.RecurringPaymentType.Manual");
            builder.Delete($"{prefix}Services.Payments.RecurringPaymentType.NotSupported");

            #endregion

            builder.AddOrUpdate("Account.Login.Fields.UsernameOrEmail")
                .Value("de", "Benutzername oder E-Mail");

            builder.AddOrUpdate("Admin.Configuration.EmailAccounts.CannotDeleteDefaultAccount")
                .Value("de", "Das Standard-E-Mail-Konto \"{0}\" kann nicht gelöscht werden. Legen Sie zunächst ein anderes Standard-E-Mail-Konto fest.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.AllowDifferingEmailAddressForEmailAFriend")
                .Value("de", "Zulassen einer anderen E-Mail-Adresse für die Weiterleitung an einen Freund");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.AllowDifferingEmailAddressForEmailAFriend.Hint")
                .Value("de", "Legt fest, ob Kunden eine andere E-Mail-Adresse angeben dürfen als die, mit der sie sich im Shop registriert haben.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.ValidateEmailAddress")
                .Value("de", "Prüfung der E-Mail-Adresse des Kunden");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.ValidateEmailAddress.Hint")
                .Value("de", "Legt fest, ob die E-Mail-Adresse des Kunden im Checkout validiert wird.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.ContactDataSettings.WebmasterEmailAddress.Hint")
                .Value("de", "Geben Sie die E-Mail-Adresse Ihres Webmasters ein.");

            builder.AddOrUpdate("Admin.OrderNotice.CustomerCompletedEmailQueued")
                .Value("de", "\"Auftrag abgeschlossen\" E-Mail (an Kunden) wurde gequeued. Queued Email ID: {0}");

            builder.AddOrUpdate("Admin.OrderNotice.CustomerEmailQueued")
                .Value("de", "\"Auftrag eingegangen\" E-Mail (an Kunden) wurde gequeued. Queued Email ID: {0}");

            builder.AddOrUpdate("Admin.System.QueuedEmails.DeleteAll.Confirm")
                .Value("de", "Sind Sie sicher, dass alle E-Mails gelöscht werden sollen?");

            builder.AddOrUpdate("Common.Error.SendMail")
                .Value("de", "Fehler beim Versenden der Email. Bitte versuchen Sie es später erneut.");

            builder.AddOrUpdate("Enums.CustomerLoginType.Email")
                .Value("de", "E-Mail");

            builder.AddOrUpdate("Enums.CustomerLoginType.UsernameOrEmail")
                .Value("de", "Benutzername oder E-Mail");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.AllowAnonymousUsersToEmailWishlist")
                .Value("de", "Gästen erlauben, ihre Wunschzettel per E-Mail zu versenden");

            builder.AddOrUpdate("Enums.TaxDisplayType.ExcludingTax")
                .Value("de", "Zzgl. Mehrwertsteuer");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.AllowAnonymousUsersToEmailAFriend")
                .Value("de", "Gästen erlauben, E-Mails an Freunde zu versenden");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.AddProductsToBasketInSinglePositions",
                "Add products to cart in single positions",
                "Produkte in einzelnen Positionen in den Warenkorb legen",
                "Enable this option if you want products with different quantities to be added to the shopping cart in single position.",
                "Aktivieren Sie diese Option, wenn Produkte mit verschiedenen Mengenangaben als Einzelpositionen in den Warenkorb gelegt werden sollen.");

            builder.AddOrUpdate("Admin.Configuration.Category.Stores.AssignToSubCategoriesAndProducts.Hint")
                .Value("de", "Diese Funktion übernimmt die Shop-Konfiguration dieser Warengruppe für alle Unterwarengruppen und Produkte. Bitte beachten Sie, dass die Änderungen an der Store-Konfiguration zunächst gespeichert werden müssen, bevor diese für Unterkategorien und Produkte übernommen werden können. <b>Vorsicht:</b> Bitte beachten Sie, <b>dass vorhandene Store-Konfigurationen überschrieben bzw. gelöscht werden</b>.");
        }
    }
}
