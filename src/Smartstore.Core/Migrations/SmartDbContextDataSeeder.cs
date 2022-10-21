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

            builder.AddOrUpdate("Admin.Catalog.BulkEdit.Fields.OldPrice",
                "Compare price",
                "Vergleichspreis",
                "Sets a comparison price, e.g.: MSRP, list price, regular price before discount, etc. The comparison price serves as the strike price.",
                "Legt einen Vergleichspreis fest, z.B.: UVP, Listenpreis, regulärer Preis vor einer Ermäßigung etc. Der Vergleichspreis dienst als Streichpreis.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.Fields.IsVerfifiedPurchase",
                "Is verified purchase",
                "Ist verifizierter Kauf",
                "Specifies whether this product review was written by a customer who purchased the product from this store.",
                "Legt fest, ob diese Produktbewertung von einem Kunden verfasst wurde, der das Produkt in diesem Shop gekauft hat.");

            builder.AddOrUpdate("Reviews.Verified", "Verified purchase", "Verifizierter Kauf");
            builder.AddOrUpdate("Reviews.Unverified", "Unverified purchase", "Nicht verifizierter Kauf");
        }
    }
}