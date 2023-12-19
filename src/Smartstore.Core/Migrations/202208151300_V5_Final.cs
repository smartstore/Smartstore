using FluentMigrator;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-08-15 13:00:00", "V5Final")]
    internal class V5Final : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.UseSmallProductBoxOnHomePage",
                "Use small product box on homepage",
                "Kleine Produktbox auf der Homepage verwenden",
                "Defines the size of the product boxes on the homepage of your store.",
                "Bestimmt die Größe der Produktboxen auf der Startseite Ihres Shops.");

            builder.AddOrUpdate("Admin.Configuration.Themes.Option.AssetCachingEnabled.Hint",
                "Determines whether compiled asset files should be cached in file system in order to speed up application restarts. Select 'Auto' if caching should depend on the current environment (Debug = disabled, Production = enabled).",
                "Legt fest, ob kompilierte JS- und CSS-Dateien wie bspw. 'Sass' im Dateisystem zwischengespeichert werden sollen, um den Programmstart zu beschleunigen. Wählen Sie 'Automatisch', wenn das Caching von der aktuellen Umgebung abhängig sein soll (Debug = deaktiviert, Produktiv = aktiviert).");

            builder.AddOrUpdate("Admin.Configuration.Themes.Option.BundleOptimizationEnabled.Hint",
                "Determines whether asset files (JS and CSS) should be grouped together in order to speed up page rendering. Select 'Auto' if bundling should depend on the current environment (Debug = disabled, Production = enabled).",
                "Legt fest, ob JS- und CSS-Dateien in Gruppen zusammengefasst werden sollen, um den Seitenaufbau zu beschleunigen. Wählen Sie 'Automatisch', wenn das Bundling von der aktuellen Umgebung abhängig sein soll (Debug = deaktiviert, Produktiv = aktiviert).");

            builder.AddOrUpdate("Admin.System.Warnings.TaskScheduler.Fail",
                "The task scheduler cannot poll and execute tasks. Base URL: {0}, Status: {1}. Please specify a working base url in appsettings.json, setting 'Smartstore.TaskSchedulerBaseUrl'.",
                "Der Task-Scheduler kann keine Hintergrund-Aufgaben planen und ausführen. Basis-URL: {0}, Status: {1}. Bitte legen Sie eine vom Webserver erreichbare Basis-URL in der Datei appsettings.json Datei fest, Einstellung: 'Smartstore.TaskSchedulerBaseUrl'.");

            builder.AddOrUpdate("Admin.Common.DataSuccessfullySaved",
                "The data was saved successfully",
                "Die Daten wurden erfolgreich gespeichert");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Standard",
                "Standard (standard splitting and filtering)",
                "Standard (Standardtrennung und -Filterung)");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Whitespace",
                "Whitespace (split only for blanks, no filtering)",
                "Whitespace (nur bei Leerzeichen trennen, keine Filterung)");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Keyword",
                "Keyword (no splitting, no filtering)",
                "Keyword (keine Trennung, keine Filterung)");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Classic",
                "Classic (classic splitting and filtering)",
                "Classic (klassische Trennung und Filterung)");

            builder.AddOrUpdate("Admin.Plugins.KnownGroup.Law", "Law", "Gesetz");

            builder.AddOrUpdate("Admin.Orders.List.PaymentId",
                "Payment ID",
                "Zahlungs-ID",
                "Search by the payment transaction ID (authorization or capturing)",
                "Suche über die Zahlungstransaktions-ID (Autorisierung oder Buchung)");

            builder.AddOrUpdate("Identity.AuthenticationCredentials", "Authentication credentials", "Zugangsdaten");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.DisplayRegionInLanguageSelector",
                "Display region in language selector",
                "Region in der Sprachauswahl anzeigen",
                "Whether to display region/country name in language selector (e.g. 'Deutsch (Deutschland)' instead of 'Deutsch')",
                "Zeigt den Namen der Region/des Landes in der Sprachauswahl an (z. B. 'Deutsch (Deutschland)' statt 'Deutsch')");

            builder.AddOrUpdate("Payment.PaymentFailure",
                "A problem has occurred with this payment method. Please try again or select another payment method.",
                "Mit dieser Zahlungsart ist ein Problem aufgetreten. Bitte versuchen Sie es erneut oder wählen Sie eine andere Zahlungsart aus.");

            builder.AddOrUpdate("Payment.MissingCheckoutState",
                "Missing checkout session state ({0}). Your payment cannot be processed. Please go to your shopping cart and checkout again.",
                "Fehlender Checkout-Sitzungsstatus ({0}). Ihre Zahlung kann leider nicht bearbeitet werden. Bitte gehen Sie zurück zum Warenkorb und Durchlaufen Sie den Checkout erneut.");

            builder.AddOrUpdate("Payment.InvalidCredentials",
                "The credentials for the payment provider are incomplete. Please enter the required credentials in the configuration area of the payment method.",
                "Die Zugangsdaten zum Zahlungsanbieter sind unvollständig. Bitte geben Sie die erforderlichen Zugangsdaten im Konfigurationsbereich der Zahlungsart ein.");

            builder.AddOrUpdate("Admin.Configuration.Payment.Methods.AddOrderNotes",
                "Create order notes",
                "Auftragsnotizen anlegen",
                "Specifies whether to create order notes when exchanging data with the payment provider.",
                "Legt fest, ob beim Datenaustausch mit dem Zahlungsanbieter Auftragsnotizen angelegt werden sollen.");

            builder.AddOrUpdate("Admin.Address.Fields.Country.MustBePublished",
                "Invalid country",
                "Ungültiges Land");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.LastDeviceFamily",
                "Last device family",
                "Zuletzt genutzte Endgerätefamilie");

            builder.AddOrUpdate("Admin.Rules.RuleOperator.LikeOperator", "Like", "Like");
            builder.AddOrUpdate("Admin.Rules.RuleOperator.NotLikeOperator", "Not like", "Not like");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.CartItemFromCategoryQuantity",
                "Product quantity from category is in range",
                "Produktmenge aus Warengruppe liegt in folgendem Bereich");

            builder.AddOrUpdate("PDFInvoice.TaxNumber").Value("en", "Tax Number:");
            builder.AddOrUpdate("PDFInvoice.VatId").Value("en", "Vat-ID:");

            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchProductType", "Product type", "Produkttyp");
            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchCategory", "Category", "Warengruppe");
            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchManufacturer", "Manufacturer", "Hersteller");
            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchProductName", "Product name", "Produktname");

            builder.AddOrUpdate("Admin.AccessDenied.DetailedDescription",
                "You do not have authorization to perform this operation. Permission: {0}, Systemname: {1}.",
                "Sie haben keine Berechtigung, diesen Vorgang durchzuführen. Zugriffsrecht: {0}, Systemname: {1}.");
        }
    }
}
