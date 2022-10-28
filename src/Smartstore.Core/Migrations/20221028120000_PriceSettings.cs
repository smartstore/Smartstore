using FluentMigrator;
using Smartstore.Core.Configuration;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-10-28 12:00:00", "Core: PriceSettings")]
    internal class PriceSettingsMigration : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
        }

        public override void Down()
        {
        }

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        /// <summary>
        /// Moves some setting properties from CatalogSettings to PriceSettings class.
        /// </summary>
        private static async Task MigrateSettingsAsync(SmartDbContext db, CancellationToken cancelToken = default)
        {
            // Move some settings from CatalogSettings to PriceSettings class
            var moveSettingProps = new[]
            {
                "ShowBasePriceInProductLists",
                "ShowVariantCombinationPriceAdjustment",
                "ShowLoginForPriceNote",
                "BundleItemShowBasePrice",
                "ShowDiscountSign",
                "PriceDisplayStyle",
                "PriceDisplayType",
                "DisplayTextForZeroPrices",
                "IgnoreDiscounts",
                "ApplyPercentageDiscountOnTierPrice",
                "ApplyTierPricePercentageToAttributePriceAdjustments"
            };
            
            foreach (var propName in moveSettingProps)
            {
                await MoveSettingAsync(db, propName);
            }

            await db.SaveChangesAsync(cancelToken);
        }

        private static async Task MoveSettingAsync(SmartDbContext db, string propName)
        {
            var sourceSettings = await db.Settings.Where(x => x.Name == $"CatalogSettings.{propName}").ToListAsync();

            foreach (var setting in sourceSettings)
            {
                db.Settings.Add(new Setting { Name = $"PriceSettings.{propName}", Value = setting.Value, StoreId = setting.StoreId });
            }

            if (sourceSettings.Count > 0)
            {
                db.Settings.RemoveRange(sourceSettings);
            }
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            // TODO: (mh) (core) Move resources also (Admin.Configuration.Settings.Catalog.* --> Admin.Configuration.Settings.Price.*)

            builder.AddOrUpdate("Admin.Configuration.Settings.Price", "Prices", "Preise");
            
            // TODO: (mh) (core) Check all of these again.
            builder.AddOrUpdate("Admin.Configuration.Settings.Price.DefaultComparePriceLabelId",
                "Default \"Compare Price\" label",
                "Standard Label für den Vergleichspreis",
                "Takes effect when a product does not define the \"Compare Price\" label.",
                "Wird wirksam, wenn für ein Produkt kein Vergleichspreis-Label ausgewählt ist.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.DefaultRegularPriceLabelId",
                "Default \"Regular Price\" label",
                "Standard Label für den regulären Preis",
                "The default price label to use for the crossed out regular price. Takes effect when there is an offer, or a discount has been applied to a product.",
                "Das Standard Label, das für den durchgestrichenen regulären Preis verwendet werden soll. Wird wirksam, wenn es einen Aktionspreis gibt oder ein Rabatt auf ein Produkt angewendet wurde.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.OfferPriceReplacesRegularPrice",
                "Special price replaces regular price",
                "Aktionspreis ersetzt regulären Preis",
                "If active the special offer price just replaces the regular price as if there was no offer. If inactive, the regular price will be displayed crossed out.",
                "Wenn aktiv, ersetzt der Aktionspreis einfach den regulären Preis als ob es kein Angebot gäbe. Wenn inaktiv, wird der reguläre Preis durchgestrichen dargestellt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.AlwaysDisplayRetailPrice",
                "Always display retail price",
                "Immer den UVP anzeigen",
                "If active, the MSRP will be displayed in product detail even if there is already an offer or a discount. " +
                "In this case the MSRP will appear as another crossed out price alongside the discounted price.",
                "Wenn aktiv, wird der UVP in den Produktdetails angezeigt, auch wenn es bereits eine Preisermäßigung gibt. " +
                "In diesem Fall wird der UVP als weiterer durchgestrichener Preis angezeigt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowOfferCountdownRemainingHours",
                "Remaining offer time after which a countdown should be displayed",
                "Angebots Restzeit, ab der ein Countdown angezeigt werden soll",
                "Sets remaining time (in hours) of the offer from which a countdown should be displayed in product detail, e.g. \"ends in 3 hours, 23 min.\". " +
                "To hide the countdown, don't enter anything. " +
                "Only applies to limited time offers with an end date.",
                "Legt die verbleibende Zeit (in Stunden) eines Angebotes fest, ab der ein Countdown im Produktdetail angezeigt werden soll, z.B. \"endet in 3 Stunden, 23 Min.\" " +
                "Um den Countdown auszublenden, lassen Sie das Feld leer. " +
                "Gilt nur für zeitlich begrenzte Angebote mit einem Enddatum.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowOfferBadge",
                "Show offer badge",
                "Zeige Badge für Angebote",
                "Displays a badge if a promotional price is active.",
                "Zeigt ein Promo Badge an, wenn ein Angebotspreis aktiv ist.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.OfferBadgeLabel",
                "Offer badge label",
                "Angebots-Badge Text",
                "The label of the offer badge, e.g. \"Deal\"",
                "Text für das Angebots-Badge, z.B. \"Deal\"");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.OfferBadgeStyle",
                "Offer badge style",
                "Angebots-Badge Stil");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.LimitedOfferBadgeLabel",
                "Limited offer badge label",
                "Angebots-Badge Text für zeitlich begrenztes Angebot",
                "The label of the offer badge if the offer is limited, e.g. \"Limited time deal\"",
                "Text für das Angebots-Badge, wenn das Angebot zeitlich befristet ist, z. B. \"Befristetes Angebot\" oder \"Nur noch kurze Zeit\"");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.LimitedOfferBadgeStyle",
                "Limited offer badge style",
                "Angebots-Badge Stil für zeitlich begrenztes Angebot");
        }
    }
}