using Smartstore.Core.Configuration;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            //await MigrateSettingsAsync(context, cancelToken);
        }

        //public async Task MigrateSettingsAsync(SmartDbContext db, CancellationToken cancelToken = default)
        //{

        //}

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.Settings.Search.CommonFacet.Sorting",
                "Sorting",
                "Sortierung",
                "Specifies the sorting of the search filters.",
                "Legt die Sortierung der Suchfilter fest.");

            builder.AddOrUpdate("Enums.FacetSorting.ValueAsc", "Value/ID: lowest first", "Wert/ID: Niedrigste zuerst");

            builder.AddOrUpdate("Admin.Common.ExportToPdf.TooManyItems",
                "Too many objects! A maximum of {0} objects can be converted. Please reduce the number of selected data records ({1}) or increase the limit in the PDF settings.",
                "Zu viele Objekte! Es können maximal {0} Objekte konvertiert werden. Bitte reduzieren Sie die Anzahl der ausgewählten Datensätze ({1}) oder erhöhen Sie das Limit in den PDF-Einstellungen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.MaxItemsToPrint",
                "Maximum number of objects to print",
                "Maximale Anzahl zu druckender Objekte",
                "Specifies the maximum number of objects to be printed, above which an error message is issued. The default value is 500 and should not be set too high so that the process does not take too long.",
                "Legt die maximale Anzahl der zu druckenden Objekte fest, bei deren Überschreitung eine Fehlermeldung ausgegeben wird. Der Standardwert ist 500 und sollte nicht zu hoch gewählt werden, damit der Vorgang nicht zu lange dauert.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Shipping.CalculateShippingAtCheckout",
                "Calculate shipping costs during checkout",
                "Versandkosten während des Checkouts berechnen",
                "Specifies whether shipping costs are displayed on the shopping cart page as long as the customer has not yet entered a shipping address. If activated, a note appears instead that the calculation will only take place at checkout.",
                "Legt fest, ob Versandkosten auf der Warenkorbseite angezeigt werden, solange der Kunde noch keine Lieferanschrift eingegeben hat. Wenn aktiviert, erscheint stattdessen ein Hinweis, dass die Berechnung erst beim Checkout erfolgt.");

            builder.AddOrUpdate("Common.CartRules", "Cart rules", "Warenkorbregeln");
            builder.AddOrUpdate("Common.CustomerRules", "Customer rules", "Kundenregeln");
            builder.AddOrUpdate("Common.ProductRules", "Product rules", "Produktregeln");
        }
    }
}