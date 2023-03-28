using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            //await MigrateSettingsAsync(context, cancelToken);
        }

        //public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        //{
        //    await context.SaveChangesAsync(cancelToken);
        //}

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

            builder.Delete("Enums.SmartStore.Core.Domain.Blogs.PreviewDisplayType.Bare");
            builder.Delete("Enums.SmartStore.Core.Domain.Blogs.PreviewDisplayType.Default");
            builder.Delete("Enums.SmartStore.Core.Domain.Blogs.PreviewDisplayType.DefaultSectionBg");
            builder.Delete("Enums.SmartStore.Core.Domain.Blogs.PreviewDisplayType.Preview");
            builder.Delete("Enums.SmartStore.Core.Domain.Blogs.PreviewDisplayType.PreviewSectionBg");
            builder.Delete("Enums.SmartStore.Core.Domain.Blogs.PreviewDisplayType.SectionBg");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeChoiceBehaviour.GrayOutUnavailable");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeChoiceBehaviour.None");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.Boxes");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.Checkboxes");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.Datepicker");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.DropdownList");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.FileUpload");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.MultilineTextbox");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.RadioList");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.AttributeControlType.TextBox");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.BackorderMode.AllowQtyBelow0");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.BackorderMode.AllowQtyBelow0AndNotifyCustomer");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.BackorderMode.NoBackorders");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.DownloadActivationType.Manually");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.DownloadActivationType.WhenOrderIsPaid");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.GiftCardType.Physical");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.GiftCardType.Virtual");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.GridColumnSpan.Max2Cols");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.GridColumnSpan.Max3Cols");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.GridColumnSpan.Max4Cols");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.GridColumnSpan.Max5Cols");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.GridColumnSpan.Max6Cols");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.LowStockActivity.DisableBuyButton");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.LowStockActivity.Nothing");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.LowStockActivity.Unpublish");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ManageInventoryMethod.DontManageStock");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ManageInventoryMethod.ManageStock");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ManageInventoryMethod.ManageStockByAttributes");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.PriceDisplayStyle.BadgeAll");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.PriceDisplayStyle.BadgeFreeProductsOnly");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.PriceDisplayStyle.Default");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.PriceDisplayType.Hide");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.PriceDisplayType.LowestPrice");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.PriceDisplayType.PreSelectedPrice");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.PriceDisplayType.PriceWithoutDiscountsAndAttributes");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductCondition.Damaged");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductCondition.New");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductCondition.Refurbished");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductCondition.Used");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.CreatedOn");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.CreatedOnAsc");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.Initial");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.NameAsc");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.NameDesc");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.PriceAsc");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.PriceDesc");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductSortingEnum.Relevance");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductType.BundledProduct");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductType.GroupedProduct");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductType.SimpleProduct");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductVariantAttributeValueType.ProductLinkage");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductVariantAttributeValueType.Simple");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductVisibility.Full");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductVisibility.Hidden");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductVisibility.ProductPage");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.ProductVisibility.SearchResults");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.RecurringProductCyclePeriod.Days");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.RecurringProductCyclePeriod.Months");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.RecurringProductCyclePeriod.Weeks");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.RecurringProductCyclePeriod.Years");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.SubCategoryDisplayType.AboveProductList");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.SubCategoryDisplayType.Bottom");
            builder.Delete("Enums.SmartStore.Core.Domain.Catalog.SubCategoryDisplayType.Hide");
            builder.Delete("Enums.SmartStore.Core.Domain.Common.PageTitleSeoAdjustment.PagenameAfterStorename");
            builder.Delete("Enums.SmartStore.Core.Domain.Common.PageTitleSeoAdjustment.StorenameAfterPagename");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerLoginType.Email");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerLoginType.Username");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerLoginType.UsernameOrEmail");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNameFormat.ShowEmails");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNameFormat.ShowFirstName");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNameFormat.ShowFullNames");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNameFormat.ShowNameAndCity");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNameFormat.ShowUsernames");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNumberMethod.AutomaticallySet");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNumberMethod.Disabled");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNumberMethod.Enabled");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNumberVisibility.Display");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNumberVisibility.Editable");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNumberVisibility.EditableIfEmpty");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.CustomerNumberVisibility.None");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.PasswordFormat.Clear");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.PasswordFormat.Encrypted");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.PasswordFormat.Hashed");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.WalletPostingReason.Admin");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.WalletPostingReason.Debit");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.WalletPostingReason.PartialRefund");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.WalletPostingReason.Purchase");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.WalletPostingReason.Refill");
            builder.Delete("Enums.SmartStore.Core.Domain.Customers.WalletPostingReason.Refund");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportAttributeValueMerging.AppendAllValuesToName");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportAttributeValueMerging.None");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDeploymentType.Email");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDeploymentType.FileSystem");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDeploymentType.Ftp");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDeploymentType.Http");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDeploymentType.PublicFolder");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.Description");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.ManufacturerAndNameAndDescription");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.ManufacturerAndNameAndShortDescription");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.NameAndDescription");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.NameAndShortDescription");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.None");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.ShortDescription");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging.ShortDescriptionOrNameIfEmpty");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportEntityType.Category");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportEntityType.Customer");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportEntityType.Manufacturer");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportEntityType.NewsLetterSubscription");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportEntityType.Order");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportEntityType.Product");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportEntityType.ShoppingCartItem");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportHttpTransmissionType.MultipartFormDataPost");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportHttpTransmissionType.SimplePost");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportOrderStatusChange.Complete");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportOrderStatusChange.None");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ExportOrderStatusChange.Processing");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ImportEntityType.Category");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ImportEntityType.Customer");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ImportEntityType.NewsLetterSubscription");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ImportEntityType.Product");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ImportFileType.CSV");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.ImportFileType.XLSX");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.RelatedEntityType.ProductVariantAttributeCombination");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.RelatedEntityType.ProductVariantAttributeValue");
            builder.Delete("Enums.SmartStore.Core.Domain.DataExchange.RelatedEntityType.TierPrice");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.CurrencyRoundingRule.AlwaysRoundDown");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.CurrencyRoundingRule.AlwaysRoundUp");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.CurrencyRoundingRule.RoundMidpointDown");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.CurrencyRoundingRule.RoundMidpointUp");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.DeliveryTimesPresentation.DateOnly");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.DeliveryTimesPresentation.LabelAndDate");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.DeliveryTimesPresentation.LabelOnly");
            builder.Delete("Enums.SmartStore.Core.Domain.Directory.DeliveryTimesPresentation.None");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountLimitationType.NTimesOnly");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountLimitationType.NTimesPerCustomer");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountLimitationType.Unlimited");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountType.AssignedToCategories");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountType.AssignedToManufacturers");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountType.AssignedToOrderSubTotal");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountType.AssignedToOrderTotal");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountType.AssignedToShipping");
            builder.Delete("Enums.SmartStore.Core.Domain.Discounts.DiscountType.AssignedToSkus");
            builder.Delete("Enums.SmartStore.Core.Domain.Localization.DefaultLanguageRedirectBehaviour.DoNoRedirect");
            builder.Delete("Enums.SmartStore.Core.Domain.Localization.DefaultLanguageRedirectBehaviour.PrependSeoCodeAndRedirect");
            builder.Delete("Enums.SmartStore.Core.Domain.Localization.DefaultLanguageRedirectBehaviour.StripSeoCode");
            builder.Delete("Enums.SmartStore.Core.Domain.Localization.InvalidLanguageRedirectBehaviour.FallbackToWorkingLanguage");
            builder.Delete("Enums.SmartStore.Core.Domain.Localization.InvalidLanguageRedirectBehaviour.ReturnHttp404");
            builder.Delete("Enums.SmartStore.Core.Domain.Localization.InvalidLanguageRedirectBehaviour.Tolerate");
            builder.Delete("Enums.SmartStore.Core.Domain.Logging.LogLevel.Debug");
            builder.Delete("Enums.SmartStore.Core.Domain.Logging.LogLevel.Error");
            builder.Delete("Enums.SmartStore.Core.Domain.Logging.LogLevel.Fatal");
            builder.Delete("Enums.SmartStore.Core.Domain.Logging.LogLevel.Information");
            builder.Delete("Enums.SmartStore.Core.Domain.Logging.LogLevel.Warning");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.CheckoutNewsLetterSubscription.Activated");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.CheckoutNewsLetterSubscription.Deactivated");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.CheckoutNewsLetterSubscription.None");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.CheckoutThirdPartyEmailHandOver.Activated");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.CheckoutThirdPartyEmailHandOver.Deactivated");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.CheckoutThirdPartyEmailHandOver.None");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.OrderStatus.Cancelled");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.OrderStatus.Complete");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.OrderStatus.Pending");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.OrderStatus.Processing");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ReturnRequestStatus.Cancelled");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ReturnRequestStatus.ItemsRefunded");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ReturnRequestStatus.ItemsRepaired");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ReturnRequestStatus.Pending");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ReturnRequestStatus.Received");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ReturnRequestStatus.RequestRejected");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ReturnRequestStatus.ReturnAuthorized");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ShoppingCartType.ShoppingCart");
            builder.Delete("Enums.SmartStore.Core.Domain.Orders.ShoppingCartType.Wishlist");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.CapturePaymentReason.OrderDelivered");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.CapturePaymentReason.OrderShipped");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.PaymentStatus.Authorized");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.PaymentStatus.Paid");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.PaymentStatus.PartiallyRefunded");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.PaymentStatus.Pending");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.PaymentStatus.Refunded");
            builder.Delete("Enums.SmartStore.Core.Domain.Payments.PaymentStatus.Voided");
            builder.Delete("Enums.SmartStore.Core.Domain.Security.PasswordFormat.Clear");
            builder.Delete("Enums.SmartStore.Core.Domain.Security.PasswordFormat.Encrypted");
            builder.Delete("Enums.SmartStore.Core.Domain.Security.PasswordFormat.Hashed");
            builder.Delete("Enums.SmartStore.Core.Domain.Security.UserRegistrationType.AdminApproval");
            builder.Delete("Enums.SmartStore.Core.Domain.Security.UserRegistrationType.Disabled");
            builder.Delete("Enums.SmartStore.Core.Domain.Security.UserRegistrationType.EmailValidation");
            builder.Delete("Enums.SmartStore.Core.Domain.Security.UserRegistrationType.Standard");
            builder.Delete("Enums.SmartStore.Core.Domain.Seo.CanonicalHostNameRule.NoRule");
            builder.Delete("Enums.SmartStore.Core.Domain.Seo.CanonicalHostNameRule.OmitWww");
            builder.Delete("Enums.SmartStore.Core.Domain.Seo.CanonicalHostNameRule.RequireWww");
            builder.Delete("Enums.SmartStore.Core.Domain.Shipping.ShippingStatus.Delivered");
            builder.Delete("Enums.SmartStore.Core.Domain.Shipping.ShippingStatus.NotYetShipped");
            builder.Delete("Enums.SmartStore.Core.Domain.Shipping.ShippingStatus.PartiallyShipped");
            builder.Delete("Enums.SmartStore.Core.Domain.Shipping.ShippingStatus.Shipped");
            builder.Delete("Enums.SmartStore.Core.Domain.Shipping.ShippingStatus.ShippingNotRequired");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.AuxiliaryServicesTaxType.HighestCartAmount");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.AuxiliaryServicesTaxType.HighestTaxRate");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.AuxiliaryServicesTaxType.ProRata");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.AuxiliaryServicesTaxType.SpecifiedTaxCategory");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.TaxBasedOn.BillingAddress");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.TaxBasedOn.DefaultAddress");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.TaxBasedOn.ShippingAddress");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.TaxDisplayType.ExcludingTax");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.TaxDisplayType.IncludingTax");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.VatNumberStatus.Empty");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.VatNumberStatus.Invalid");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.VatNumberStatus.Unknown");
            builder.Delete("Enums.SmartStore.Core.Domain.Tax.VatNumberStatus.Valid");
            builder.Delete("Enums.SmartStore.Core.Search.Facets.FacetSorting.DisplayOrder");
            builder.Delete("Enums.SmartStore.Core.Search.Facets.FacetSorting.HitsDesc");
            builder.Delete("Enums.SmartStore.Core.Search.Facets.FacetSorting.LabelAsc");
            builder.Delete("Enums.SmartStore.Core.Search.Facets.FacetSorting.ValueAsc");
            builder.Delete("Enums.SmartStore.Core.Search.Facets.FacetTemplateHint.Checkboxes");
            builder.Delete("Enums.SmartStore.Core.Search.Facets.FacetTemplateHint.Custom");
            builder.Delete("Enums.SmartStore.Core.Search.Facets.FacetTemplateHint.NumericRange");
            builder.Delete("Enums.SmartStore.Core.Search.IndexingStatus.Idle");
            builder.Delete("Enums.SmartStore.Core.Search.IndexingStatus.Rebuilding");
            builder.Delete("Enums.SmartStore.Core.Search.IndexingStatus.Unavailable");
            builder.Delete("Enums.SmartStore.Core.Search.IndexingStatus.Updating");
            builder.Delete("Enums.SmartStore.Core.Search.SearchMode.Contains");
            builder.Delete("Enums.SmartStore.Core.Search.SearchMode.ExactMatch");
            builder.Delete("Enums.SmartStore.Core.Search.SearchMode.StartsWith");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.DropoffType.BusinessServiceCenter");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.DropoffType.DropBox");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.DropoffType.RegularPickup");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.DropoffType.RequestCourier");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.DropoffType.Station");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.PackingType.PackByDimensions");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.PackingType.PackByOneItemPerPackage");
            builder.Delete("Enums.SmartStore.Plugin.Shipping.Fedex.PackingType.PackByVolume");
            builder.Delete("Enums.SmartStore.Rules.RuleScope.Cart");
            builder.Delete("Enums.SmartStore.Rules.RuleScope.Cart.Hint");
            builder.Delete("Enums.SmartStore.Rules.RuleScope.Customer");
            builder.Delete("Enums.SmartStore.Rules.RuleScope.Customer.Hint");
            builder.Delete("Enums.SmartStore.Rules.RuleScope.OrderItem");
            builder.Delete("Enums.SmartStore.Rules.RuleScope.Product");
            builder.Delete("Enums.SmartStore.Rules.RuleScope.Product.Hint");
            builder.Delete("Enums.SmartStore.Services.Payments.RecurringPaymentType.Automatic");
            builder.Delete("Enums.SmartStore.Services.Payments.RecurringPaymentType.Manual");
            builder.Delete("Enums.SmartStore.Services.Payments.RecurringPaymentType.NotSupported");

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
        }
    }
}