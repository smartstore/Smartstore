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
            builder.AddOrUpdate("Admin.System.SystemInfo.DataProviderFriendlyName", "Database", "Datenbank");
            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountLimitationsInLists",
                "Validate discount limitations in product lists",
                "Prüfe die Rabattgültigkeit in Produktlisten",
                "Enabling this option may reduce the performance.",
                "Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountGiftCardsInLists",
                "Check cart for gift cards when validating discounts in product lists",
                "Prüfe den Warenkorb auf Gutscheine bei der Rabattvalidierung in Produktlisten",
                "Specifies whether to check the shopping cart for the existence of gift cards when validating discounts in product lists. In case of gift cards no discount is applied because the customer could earn money through that. Enabling this option may reduce the performance.",
                "Legt fest, ob bei der Rabattvalidierung in Produktlisten der Warenkorb auf vorhandene Gutscheine überprüft werden soll. Bei Gutscheinen wird kein Rabatt gewährt, weil der Kunde damit Geld verdienen könnte. Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ValidateDiscountRulesInLists",
                "Validate cart rules of discounts in product lists",
                "Prüfe Warenkorbregeln von Rabatten in Produktlisten",
                "Enabling this option may reduce the performance.",
                "Die Aktivierung dieser Option kann die Performance beeinträchtigen.");

            builder.AddOrUpdate("Common.Entity.SelectProducts", "Select products", "Produkte auswählen");
            builder.AddOrUpdate("Common.Entity.SelectCategories", "Select categories", "Warengruppen auswählen");
            builder.AddOrUpdate("Common.Entity.SelectManufacturers", "Select manufacturers", "Hersteller auswählen");
            builder.AddOrUpdate("Common.Entity.SelectTopics", "Select topics", "Seiten auswählen");

            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo", "Table statistics", "Tabellenstatistik");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.TableName", "Table name", "Tabelle");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.NumRows", "Rows", "Datensätze");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.TotalSpace", "Total space", "Gesamtgröße");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.UsedSpace", "Used space", "Genutzt");
            builder.AddOrUpdate("Admin.System.SystemInfo.DbTableInfo.UnusedSpace", "Unused", "Ungenutzt");

            builder.AddOrUpdate("Admin.Configuration.Settings.Media.OffloadEmbeddedImagesOnSave",
                "Offload embedded Base64 images on save",
                "Eingebettete Base64-Bilder beim Speichern extrahieren",
                "Finds embedded Base64 images in long HTML descriptions, extracts and saves them to the media storage, and replaces the Base64 fragment with the media path. Offloading is automatically triggered by saving an entity to the database. Currently supported entity types are: Product, Category, Manufacturer and Topic.",
                "Findet eingebettete Base64-Bilder in langen HTML-Beschreibungen, extrahiert und speichert sie im Medienspeicher und ersetzt das Base64-Fragment durch den Medienpfad. Die Extraktion wird automatisch ausgelöst, wenn eine Entität in der Datenbank gespeichert wird. Derzeit unterstützte Entitätstypen sind: Produkt, Warengruppe, Hersteller und Seite.");

            builder.AddOrUpdate("Admin.Configuration.Plugins.Description.Step1",
                "Use the <a id='{0}' href='{1}' data-toggle='modal'>package uploader</a> or upload the plugin manually - eg. via FTP - to the <i>/Modules</i> folder in your Smartstore directory.",
                "Verwenden Sie den <a id='{0}' href='{1}' data-toggle='modal'>Paket Uploader</a> oder laden Sie das Plugin manuell - bspw. per FTP - in den <i>/Modules</i> Ordner hoch.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Search.SearchFieldsNote",
                "The Name, SKU and Short Description fields can be searched in the standard search. Other fields require a search plugin such as the MegaSearch plugin from <a href='https://smartstore.com/en/editions-prices' target='_blank'>Premium Edition</a>.",
                "In der Standardsuche können die Felder Name, SKU und Kurzbeschreibung durchsucht werden. Für weitere Felder ist ein Such-Plugin wie etwa das MegaSearch-Plugin aus der <a href='https://smartstore.com/en/editions-prices' target='_blank'>Premium Edition</a> notwendig.");
        }
    }
}