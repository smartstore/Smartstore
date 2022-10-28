using FluentMigrator;
using Smartstore.Core.Configuration;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-10-28 12:00:00", "Core: PriceSettings")]
    internal class PriceSettings : Migration, IDataSeeder<SmartDbContext>
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

        public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            var settings = context.Set<Setting>();

            await MigrateSettingAsync(settings, "CatalogSettings.ShowBasePriceInProductLists", "PriceSettings.ShowBasePriceInProductLists", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.ShowVariantCombinationPriceAdjustment", "PriceSettings.ShowVariantCombinationPriceAdjustment", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.ShowLoginForPriceNote", "PriceSettings.ShowLoginForPriceNote", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.BundleItemShowBasePrice", "PriceSettings.BundleItemShowBasePrice", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.ShowDiscountSign", "PriceSettings.ShowDiscountSign", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.PriceDisplayStyle", "PriceSettings.PriceDisplayStyle", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.PriceDisplayType", "PriceSettings.PriceDisplayType", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.DisplayTextForZeroPrices", "PriceSettings.DisplayTextForZeroPrices", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.IgnoreDiscounts", "PriceSettings.IgnoreDiscounts", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.ApplyPercentageDiscountOnTierPrice", "PriceSettings.ApplyPercentageDiscountOnTierPrice", cancelToken);
            await MigrateSettingAsync(settings, "CatalogSettings.ApplyTierPricePercentageToAttributePriceAdjustments", "PriceSettings.ApplyTierPricePercentageToAttributePriceAdjustments", cancelToken);

            await context.SaveChangesAsync(cancelToken);
        }

        private async Task MigrateSettingAsync(
            DbSet<Setting> dbSetSettings,
            string oldSettingName,
            string newSettingName,
            CancellationToken cancelToken = default)
        {
            var settings = await dbSetSettings.Where(x => x.Name == oldSettingName).ToListAsync(cancelToken);

            foreach (var setting in settings)
            {
                dbSetSettings.Add(new Setting { Name = newSettingName, Value = setting.Value, StoreId = setting.StoreId });
            }

            if (settings.Count > 0)
            {
                dbSetSettings.RemoveRange(settings);
            }
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.PriceSettings", "Prices", "Preise");
            
            // TODO: (mh) (core) Check all of these again.
            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultComparePriceLabelId",
                "Default compare price label",
                "Standardpreis-Label für den Vergleichspreis",
                "The default price label for product compare prices. Takes effect when a product does not define the compare price label.",
                "Das Standardpreis-Label für Vergleichspreise. Tritt in Kraft, wenn für ein Produkt kein Vergleichspreis-Label definiert ist.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DefaultRegularPriceLabelId",
                "Default regular price label",
                "Standardpreis-Label für den regulären Preis",
                "The default price label to use for the crossed out regular price. Takes effect when there is an offer or a discount has been applied to a product.",
                "Das Standardpreis-Label, das für den durchgestrichenen regulären Preis verwendet wird. Tritt in Kraft, wenn es einen Aktionspreis gibt oder ein Rabatt auf ein Produkt angewendet wurde.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.OfferPriceReplacesRegularPrice",
                "Offer price replaces regular price",
                "Aktionspreis ersetzt regulären Preis",
                "If set to true the special offer price just replaces the regular price as if there was no offer. If set to false, the regular price will be displayed crossed out.",
                "Bei Ja ersetzt der Preis des Sonderangebots einfach den regulären Preis, als ob es kein Angebot gäbe. Bei Nein wird der reguläre Preis durchgestrichen angezeigt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.AlwaysDisplayRetailPrice",
                "Always display retail price",
                "Immer den UVP anzeigen",
                "If set to true, the MSRP will be displayed in product detail even if there is already an offer or a discount. " +
                "In this case the MSRP will appear as another crossed out price alongside the discounted price.",
                "Wenn diese Option auf Ja gesetzt ist, wird der MSRP in den Produktdetails angezeigt, auch wenn es bereits ein Angebot oder einen Rabatt gibt. " +
                "In diesem Fall wird der MSRP als weiterer durchgestrichener Preis neben dem rabattierten Preis angezeigt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowOfferCountdownRemainingHours",
                "Show offer countdown remaining hours",
                "Verbleibende Stunden des Angebots anzeigen",
                "Sets remaining time (in hours) of the offer from which a countdown should be displayed in product detail, e.g. ends in 3 hours, 23 min. " +
                "To hide the countdown, unset this setting. " +
                "Only applies to limited time offers with a defined end date.",
                "Legt die verbleibende Zeit (in Stunden) des Angebots fest, ab der ein Countdown im Produktdetail angezeigt werden soll, z.B. endet in 3 Stunden, 23 Minuten. " +
                "Um den Countdown auszublenden, deaktivieren Sie diese Einstellung. " +
                "Gilt nur für zeitlich begrenzte Angebote mit einem definierten Enddatum.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowOfferBadge",
                "Show offer badge",
                "Zeige Label für Angebot",
                "Specifies whether to display a badge if an offer price is active.",
                "Legt fest, ob ein Badge angezeigt werden soll, wenn ein Angebotspreis vorhanden ist.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.OfferBadgeLabel",
                "Offer badge label",
                "Text für Angebotslabel",
                "The label of the offer badge, e.g. Deal",
                "Label für den Angebotspreis z.B. Deal");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.OfferBadgeStyle",
                "Offer badge style",
                "Badge-Style für Angebot");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LimitedOfferBadgeLabel",
                "Limited offer badge label",
                "Label für zeitlich begrenztes Angebot",
                "The label of the offer badge if the offer is limited, e.g. Limited time deal",
                "Das Label der Badge, wenn das Angebot zeitlich begrenzt ist, z. B. Befristetes Angebot");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LimitedOfferBadgeStyle",
                "Limited offer badge style",
                "Badge-Style für zeitlich begrenztes Angebot",
                "The style of the limited time offer badge.",
                "Badge-Style des Labels für das zeitlich begrenzte Angebot.");
        }
    }
}