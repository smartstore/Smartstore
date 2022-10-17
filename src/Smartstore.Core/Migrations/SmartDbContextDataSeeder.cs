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
        }
    }
}