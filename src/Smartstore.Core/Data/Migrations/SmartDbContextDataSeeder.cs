using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Seo;
using Smartstore.Data.Migrations;
using Smartstore.Utilities;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            //await context.SaveChangesAsync(cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {   
        }
    }
}