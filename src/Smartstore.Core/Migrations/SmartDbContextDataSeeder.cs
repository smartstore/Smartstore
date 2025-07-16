using Org.BouncyCastle.Utilities;
using Smartstore.Data.Migrations;
using static Smartstore.Core.Security.Permissions;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            return Task.CompletedTask;
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Aria.Label.AlphabeticallySortedLinks", "Alphabetically sorted links", "Alphabetisch sortierte Links");
            builder.AddOrUpdate("Aria.Label.BundleContains", "The product set contains {0}", "Das Produktset enthält {0}");

            builder.AddOrUpdate("Aria.Label.CartItemSummary", 
                "{0} at {1}.",
                "{0} zu {1}.");
            builder.AddOrUpdate("Aria.Label.CartItemSummaryWithTotal",
                "{0} at {1} each, quantity {2}, total {3}.",
                "{0} zu je {1}, Menge {2}, Gesamt {3}.");
            builder.AddOrUpdate("Aria.Label.CartTotalSummary",
                "Your order: {0} {1}, {2} products.",
                "Ihre Bestellung: {0} {1}, {2} Artikel.");

            builder.AddOrUpdate("Homepage.TopCategories", "Top categories", "Top-Warengruppen");
            builder.AddOrUpdate("Common.SkipList", "Skip list", "Liste überspringen");

            builder.AddOrUpdate("Products.ProductsHaveBeenAddedToTheCart",
                "{0} of {1} products have been added to the shopping cart.",
                "{0} von {1} Produkten wurden in den Warenkorb gelegt.");

            builder.Delete(
                "Media.Category.ImageAlternateTextFormat",
                "Media.Manufacturer.ImageAlternateTextFormat",
                "Media.Product.ImageAlternateTextFormat",
                "Common.DecreaseValue",
                "Common.IncreaseValue");

            builder.AddOrUpdate("ShoppingCart.DiscountCouponCode.Removed", "The discount code has been removed", "Der Rabattcode wurde entfernt");
            builder.AddOrUpdate("ShoppingCart.GiftCardCouponCode.Removed", "The gift card code has been removed", "Der Geschenkgutschein wurde entfernt");
            builder.AddOrUpdate("ShoppingCart.RewardPoints.Applied", "The reward points were applied.", "Die Bonuspunkte wurden angewendet.");
            builder.AddOrUpdate("ShoppingCart.RewardPoints.Removed", "The reward points have been removed.", "Die Bonuspunkte wurden entfernt.");

            // Resource value was a bit off.
            builder.AddOrUpdate("ShoppingCart.DiscountCouponCode.Tooltip", "Your discount code", "Ihr Rabattcode");
        }
    }
}