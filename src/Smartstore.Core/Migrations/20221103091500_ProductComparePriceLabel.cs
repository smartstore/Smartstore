using System.Data;
using FluentMigrator;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-11-03 09:15:00", "Core: ProductComparePriceLabel")]
    internal class ProductComparePriceLabel : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string FkName = "FK_Product_PriceLabel_ComparePriceLabelId";
        const string IxName = "IX_Product_ComparePriceLabelId";
        const string ProductTable = nameof(Product);
        const string DiscountTable = nameof(Discount);
        const string LabelIdColumn = nameof(Product.ComparePriceLabelId);
        const string RemainingHoursColumn = nameof(Discount.ShowCountdownRemainingHours);
        const string BadgeLabelColumn = nameof(Discount.OfferBadgeLabel);

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public override void Up()
        {
            if (!Schema.Table(ProductTable).Column(LabelIdColumn).Exists())
            {
                Create.Column(LabelIdColumn).OnTable(ProductTable).AsInt32().Nullable()
                    .Indexed(IxName)
                    .ForeignKey(FkName, nameof(PriceLabel), nameof(BaseEntity.Id))
                    .OnDelete(Rule.SetNull);
            }

            if (!Schema.Table(DiscountTable).Column(RemainingHoursColumn).Exists())
            {
                Create.Column(RemainingHoursColumn).OnTable(DiscountTable).AsInt32().Nullable();
            }

            if (!Schema.Table(DiscountTable).Column(BadgeLabelColumn).Exists())
            {
                Create.Column(BadgeLabelColumn).OnTable(DiscountTable).AsString(50).Nullable();
            }
        }

        public override void Down()
        {
            // INFO: AutoReversingMigration does not down-migrate anything here. It just removes the version info.

            var products = Schema.Table(ProductTable);
            var discounts = Schema.Table(DiscountTable);

            if (products.Index(IxName).Exists())
            {
                Delete.Index(IxName).OnTable(ProductTable);
            }

            if (products.Constraint(FkName).Exists())
            {
                Delete.ForeignKey(FkName).OnTable(ProductTable);
            }

            if (products.Column(LabelIdColumn).Exists())
            {
                Delete.Column(LabelIdColumn).FromTable(ProductTable);
            }

            if (discounts.Column(RemainingHoursColumn).Exists())
            {
                Delete.Column(RemainingHoursColumn).FromTable(DiscountTable);
            }

            if (discounts.Column(BadgeLabelColumn).Exists())
            {
                Delete.Column(BadgeLabelColumn).FromTable(DiscountTable);
            }
        }

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Catalog.Products.Fields.ComparePriceLabelId",
                "\"Compare Price\" label",
                "Label für den Vergleichspreis",
                "Specifies the text for the \"Compare Price\" label. This value overwrites the system default setting.",
                "Legt den Text für den Vergleichspreis-Label fest. Dieser Wert überschreibt die Standardeinstellung des Systems.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.Fields.ShowCountdownRemainingHours",
                "Remaining offer time after which a countdown should be displayed",
                "Angebotsrestzeit, ab der ein Countdown angezeigt werden soll",
                "Sets remaining time (in hours) of the offer from which a countdown should be displayed on product detail pages, e.g. \"ends in 3 hours, 23 min.\". " +
                "Only applies to limited time discounts with an end date. This value overwrites the system default setting.",
                "Legt die verbleibende Zeit (in Stunden) eines Angebotes fest, ab der ein Countdown auf der Produktdetailseite angezeigt werden soll, z.B. \"endet in 3 Stunden, 23 Min.\". " +
                "Gilt nur für zeitlich begrenzte Rabatte mit einem Enddatum. Dieser Wert überschreibt die Standardeinstellung des Systems.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.Fields.OfferBadgeLabel",
                "Offer badge label",
                "Angebots-Badge Text",
                "The label of the offer badge, e.g. \"Deal\". This value overwrites the system default setting.",
                "Text für das Angebots-Badge, z.B. \"Deal\". Dieser Wert überschreibt die Standardeinstellung des Systems.");
        }
    }
}
