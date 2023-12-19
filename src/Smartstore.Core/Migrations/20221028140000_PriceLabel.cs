using FluentMigrator;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-10-28 14:00:00", "Core: PriceLabels")]
    internal class PriceLabelInitial : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
            var propTableName = nameof(PriceLabel);
            if (!Schema.Table(propTableName).Exists())
            {
                Create.Table(propTableName)
                    .WithIdColumn()
                    .WithColumn(nameof(PriceLabel.ShortName)).AsString(16).NotNullable()
                    .WithColumn(nameof(PriceLabel.Name)).AsString(50).Nullable()
                    .WithColumn(nameof(PriceLabel.Description)).AsString(400).Nullable()
                    .WithColumn(nameof(PriceLabel.IsRetailPrice)).AsBoolean()
                    .WithColumn(nameof(PriceLabel.DisplayShortNameInLists)).AsBoolean()
                    .WithColumn(nameof(PriceLabel.DisplayOrder)).AsInt32();
            }
        }

        public override void Down()
        {
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);

            var defaultLanguage = await context.Languages.OrderBy(x => x.DisplayOrder).FirstOrDefaultAsync(cancellationToken: cancelToken);
            var priceLabels = context.Set<PriceLabel>();

            var msrpPriceLabel = new PriceLabel();
            var lowestPriceLabel = new PriceLabel();
            var regularPriceLabel = new PriceLabel();

            if (defaultLanguage.UniqueSeoCode == "de")
            {
                msrpPriceLabel = new PriceLabel
                {
                    ShortName = "UVP",
                    Name = "Unverb. Preisempf.",
                    Description = "Die UVP ist der vorgeschlagene oder empfohlene Verkaufspreis eines Produkts, wie er vom Hersteller angegeben und vom Hersteller, einem Lieferanten oder Händler zur Verfügung gestellt wird.",
                    IsRetailPrice = true,
                    DisplayShortNameInLists = true
                };

                lowestPriceLabel = new PriceLabel
                {
                    ShortName = "Niedrigster",
                    Name = "Zuletzt niedrigster Preis",
                    Description = "Es handelt sich um den niedrigsten Preis des Produktes in den letzten 30 Tagen vor der Anwendung der Preisermäßigung.",
                    DisplayShortNameInLists = true
                };

                regularPriceLabel = new PriceLabel
                {
                    ShortName = "Regulär",
                    Name = "Regulär",
                    Description = "Es handelt sich um den mittleren Verkaufspreis, den Kunden für ein Produkt in unserem Shop zahlen, ausgenommen Aktionspreise."
                };
            }
            else
            {
                msrpPriceLabel = new PriceLabel
                {
                    ShortName = "MSRP",
                    Name = "Suggested retail price",
                    Description = "The Suggested Retail Price (MSRP) is the suggested or recommended retail price of a product set by the manufacturer and provided by a manufacturer, supplier, or seller.",
                    IsRetailPrice = true,
                    DisplayShortNameInLists = true
                };

                lowestPriceLabel = new PriceLabel
                {
                    ShortName = "Lowest",
                    Name = "Lowest recent price",
                    Description = "This is the lowest price of the product in the past 30 days prior to the application of the price reduction.",
                    DisplayShortNameInLists = true
                };

                regularPriceLabel = new PriceLabel
                {
                    ShortName = "Regular",
                    Name = "Regular price",
                    Description = "The Regular Price is the median selling price paid by customers for a product, excluding promotional prices"
                };
            }

            priceLabels.AddRange(msrpPriceLabel, lowestPriceLabel, regularPriceLabel);

            await context.SaveChangesAsync(cancelToken);

            var defaultComparePriceLabelIdSettings = await context.Settings
                .Where(x => x.Name == "PriceSettings.DefaultComparePriceLabelId")
                .FirstOrDefaultAsync(cancellationToken: cancelToken);

            var defaultRegularPriceLabelIdSettings = await context.Settings
                .Where(x => x.Name == "PriceSettings.DefaultRegularPriceLabelId")
                .FirstOrDefaultAsync(cancellationToken: cancelToken);

            defaultComparePriceLabelIdSettings.Value = msrpPriceLabel.Id.ToString();
            defaultRegularPriceLabelIdSettings.Value = lowestPriceLabel.Id.ToString();

            await context.SaveChangesAsync(cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Permissions.DisplayName.PriceLabel", "Price labels", "Preis Labels");

            builder.AddOrUpdate("Admin.Configuration.PriceLabels", "Price Labels", "Preis Labels");
            builder.AddOrUpdate("Admin.Configuration.PriceLabels.EditPriceLabelDetails", "Edit Price Label", "Preis Label bearbeiten");
            builder.AddOrUpdate("Admin.Configuration.PriceLabels.AddNew", "Add Price Label", "Preis Label hinzufügen");
            builder.AddOrUpdate("Admin.Configuration.PriceLabel.Added", "Price label was successfully added.", "Das Preis Label wurde erfolgreich zugefügt.");
            builder.AddOrUpdate("Admin.Configuration.PriceLabel.Updated", "Price label was successfully updated.", "Das Preis Label wurde erfolgreich aktualisiert.");
            
            builder.AddOrUpdate("Admin.Configuration.PriceLabel.Fields.Name",
                "Name",
                "Name",
                "Specifies the optional name that is displayed on product detail pages, e.g. 'Retail price', 'Lowest recent price'.",
                "Der optionale Name, der auf der Produktdetailseite angezeigt wird, z.B. 'Unverb. Preisempf.', 'Zuletzt niedrigster Preis'.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.Fields.ShortName",
                "Name (short)",
                "Name (kurz)",
                "The short name that is displayed in product lists, e.g. 'MSRP', 'Lowest'.",
                "Kurzbezeichnung, die in Produktlisten angezeigt wird, z.B. 'UVP', 'Niedrigster'.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.Fields.Description",
                "Description",
                "Beschreibung",
                "The optional description that is displayed on product detail pages in a tooltip.",
                "Optionale Beschreibung, die auf Produktdetailseiten in einem Tooltip angezeigt wird.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.Fields.IsRetailPrice",
                "Is MSRP",
                "Ist UVP",
                "Specifies whether this label represents an MSRP price.",
                "Gibt an, ob dieses Label einen UVP-Preis darstellt.");

            builder.AddOrUpdate("Admin.Configuration.PriceLabel.Fields.DisplayShortNameInLists",
                "Display short name in lists",
                "Kurznamen in Listen anzeigen",
                "Specifies whether the label's short name should be displayed in product lists.",
                "Gibt an, ob die Kurzbezeichnung des Labels in Produktlisten angezeigt werden soll.");
        }
    }
}