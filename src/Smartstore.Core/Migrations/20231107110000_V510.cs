using FluentMigrator;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-11-07 11:00:00", "V510")]
    internal class V510 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            await context.MigrateLocaleResourcesAsync(builder =>
            {
                // Lower case resource name fix for SQLite. Added resource in pascal case in MigrateLocaleResources below.
                builder.Delete("admin.system.systeminfo.appversion", "admin.system.systeminfo.appversion.hint");
            });

            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public async Task MigrateSettingsAsync(SmartDbContext db, CancellationToken cancelToken = default)
        {
            await db.MigrateSettingsAsync(builder =>
            {
                builder.Update<MediaSettings>(x => x.ProductDetailsPictureSize, 680, 600);

                builder.Delete(
                    "PrivacySettings.EnableCookieConsent",
                    "CatalogSettings.ShowShareButton",
                    "CatalogSettings.PageShareCode");
            });

            // Remove duplicate settings for PrivacySettings.CookieConsentRequirement
            var storeIds = await db.Stores
                .Select(x => x.Id)
                .ToListAsync(cancelToken);

            var cookieConsentRequirementSettings = await db.Settings
                .Where(x => x.Name == "PrivacySettings.CookieConsentRequirement")
                .ToListAsync(cancelToken);

            if (cookieConsentRequirementSettings.Count > storeIds.Count)
            {
                foreach (var storeId in storeIds)
                {
                    var storeSpecificSettings = cookieConsentRequirementSettings
                        .Where(x => x.StoreId == storeId)
                        .ToList();

                    if (storeSpecificSettings.Count > 1)
                    {
                        db.Settings.RemoveRange(storeSpecificSettings.Skip(1));
                    }
                }

                var settingsForAllStores = cookieConsentRequirementSettings
                    .Where(x => x.StoreId == 0)
                    .ToList();

                if (settingsForAllStores.Count > 1)
                {
                    db.Settings.RemoveRange(settingsForAllStores.Skip(1));
                }
            }

            await db.SaveChangesAsync(cancelToken);

            await MigrateOfflinePaymentDescriptions(db, cancelToken);
            await MigrateImportProfileColumnMappings(db, cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.Settings.Order.GiftCards_Activated")
                .Value("de", "Geschenkgutschein ist aktiviert, wenn Auftragsstatus...");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.GiftCards_Activated.Hint")
                .Value("de", "Legt den Auftragsstatus einer Bestellung fest, bei dem in der Bestellung enthaltene Geschenkgutscheine automatisch aktiviert werden.");

            builder.Delete(
                "Admin.Configuration.Currencies.GetLiveRates",
                "Common.Error.PreProcessPayment",
                "Payment.PayingFailed",
                "Enums.BackorderMode.AllowQtyBelow0AndNotifyCustomer",
                "Admin.Catalog.Attributes.CheckoutAttributes.Values.SaveBeforeEdit");

            builder.AddOrUpdate("Enums.DataExchangeCompletionEmail.Always", "Always", "Immer");
            builder.AddOrUpdate("Enums.DataExchangeCompletionEmail.OnError", "If an error occurs", "Bei einem Fehler");
            builder.AddOrUpdate("Enums.DataExchangeCompletionEmail.Never", "Never", "Nie");

            builder.AddOrUpdate("Admin.Configuration.Settings.DataExchange.ImportCompletionEmail",
                "Import completion email",
                "E-Mail zum Importabschluss",
                "Specifies whether an email should be sent when an import has completed.",
                "Legt fest, ob eine E-Mail bei Abschluss eines Imports verschickt werden soll.");

            builder.Update("Admin.Configuration.Payment.Methods.Fields.RecurringPaymentType").Value("de", "Abo Zahlungen");
            builder.Update("Admin.Plugins.LicensingDemoRemainingDays").Value("de", "Demo: noch {0} Tag(e)");

            builder.AddOrUpdate("Admin.ContentManagement.MessageTemplates.Fields.BccEmailAddresses.Hint",
                "BCC address. The BCC (blind carbon copy) field contains one or more semicolon-separated email addresses to which a copy of the email is sent without being visible to the other specified recipients.",
                "BCC-Adresse. Das BCC-Feld enthält eine oder mehrere durch Semikolon getrennte E-Mail-Adressen, an die eine Kopie der E-Mail gesendet wird, ohne dass das für die anderen angegebenen Empfänger sichtbar sein soll („Blindkopie“).");

            builder.AddOrUpdate("Enums.BackorderMode.AllowQtyBelow0OnBackorder",
                "Allow quantity below 0. Delivered as soon as in stock.",
                "Menge kleiner als 0 zulassen. Wird nachgeliefert, sobald auf Lager.");

            builder.Update("Enums.BackorderMode.AllowQtyBelow0").Value("en", "Allow quantity below 0");

            builder.AddOrUpdate("Common.Pageviews", "Page views", "Seitenaufrufe");
            builder.AddOrUpdate("Common.PageviewsCount", "{0} page views", "{0} Seitenaufrufe");

            builder.AddOrUpdate("Enums.CapturePaymentReason.OrderCompleted",
                "The order has been marked as completed",
                "Der Auftrag wurde als abgeschlossen markiert");

            builder.AddOrUpdate("Admin.Common.Delete.Selected", "Delete selected", "Ausgewählte löschen");

            builder.AddOrUpdate("Common.RecycleBin", "Recycle bin", "Papierkorb");
            builder.AddOrUpdate("Common.Restore", "Restore", "Wiederherstellen");
            builder.AddOrUpdate("Common.DeletePermanent", "Delete permanently", "Endgültig löschen");
            builder.AddOrUpdate("Common.NumberOfOrders", "Number of orders", "Auftragsanzahl");

            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.Clear", "Empty recycle bin", "Papierkorb leeren");
            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.ClearConfirm",
                "Are you sure that all entries of the recycle bin should be deleted?",
                "Sind Sie sicher, dass alle Einträge des Papierkorbs gelöscht werden sollen?");

            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.AdminNote",
                "A recovery of deleted products is intended for emergencies. Some data cannot be restored in the process. These include assignments to delivery times and quantity units, country of origin and the compare price label (e.g. RRP). Products that are assigned to orders are ignored during deletion, as they cannot be deleted permanently.",
                "Eine Wiederherstellung von gelöschten Produkten ist für Notfälle gedacht. Einige Daten können dabei nicht wiederhergestellt werden. Dazu zählen Zuordnungen zu Lieferzeiten und Verpackungseinheiten, Herkunftsland und der Vergleichspreiszusatz (z.B. UVP). Produkte, die Aufträgen zugeordnet sind, werden beim Löschen ignoriert, da sie nicht endgültig gelöscht werden können.");

            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.ProductWithAssignedOrdersWarning",
                "The product is assigned to {0} orders. A product cannot be permanently deleted if it is assigned to an order.",
                "Das Produkt ist {0} Aufträgen zugeordnet. Ein Produkt kann nicht permanent gelöscht werden, wenn es einem Auftrag zugeordnet ist.");

            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.DeleteProductsResult",
                "{0} of {1} products have been permanently deleted. {2} products were skipped.",
                "Es wurden {0} von {1} Produkten entgültig gelöscht. {2} Produkte wurden übersprungen.");

            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.RestoreProductsResult",
                "{0} of {1} products were successfully restored.",
                "Es wurden {0} von {1} Produkten erfolgreich wiederhergestellt.");

            builder.AddOrUpdate("Admin.Packaging.InstallSuccess",
                "Package '{0}' was uploaded and unzipped successfully. Please click Edit / Reload list of plugins.",
                "Paket '{0}' wurde hochgeladen und erfolgreich entpackt. Bitte klicken Sie auf Bearbeiten / Plugin-Liste erneut laden.");

            builder.AddOrUpdate("Account.Fields.Newsletter",
                "I would like to subscribe to the newsletter. I agree to the <a href=\"{0}\">Privacy policy</a>. Unsubscription is possible at any time.",
                "Ich möchte den Newsletter abonnieren. Mit den Bestimmungen zum <a href=\"{0}\">Datenschutz</a> bin ich einverstanden. Eine Abmeldung ist jederzeit möglich.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.Login",
                "Login & Registration",
                "Login & Registrierung");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.Visibility",
                "Visibility",
                "Sichtbarkeit");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.Misc",
                "Miscellaneous",
                "Sonstiges");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CheckUsernameAvailabilityEnabled",
                "Enable username availability check",
                "Verfügbarkeitsprüfung des Benutzernamens");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.HideDownloadableProductsTab",
                "Hide downloads in the \"My account\" area",
                "Downloads im Bereich \"Mein Konto\" ausblenden");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerNameAllowedCharacters",
                "Additional allowed characters for customer names",
                "Zusätzlich erlaubte Zeichen für Kundennamen",
                "Add additional characters here that are permitted when entering a user name. Leave the field blank to allow all characters.",
                "Fügen Sie hier zusätzliche Zeichen hinzu, die bei der Eingabe eines Benutzernamens zulässig sind. Lassen Sie das Feld leer, um alle Zeichen zuzulassen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerNameAllowedCharacters.AdminHint",
                "The following characters are already allowed by default:",
                "Standardmäßig sind bereits folgende Zeichen erlaubt:");

            builder.AddOrUpdate("Admin.DataGrid.ConfirmSoftDelete",
                "Do you want to delete the item?",
                "Soll der Datensatz gelöscht werden?");

            builder.AddOrUpdate("Admin.DataGrid.ConfirmSoftDeleteMany",
                "Do you want to delete the selected {0} items?",
                "Sollen die gewählten {0} Datensätze gelöscht werden?");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowShortDescriptionInGridStyleLists.Hint",
                "Specifies whether the product short description should be displayed in product lists. This setting only applies to the grid view display.",
                "Legt fest, ob die Produkt-Kurzbeschreibung auch in Produktlisten angezeigt werden sollen. Diese Einstellungsmöglichkeit bezieht sich nur auf die Darstellung in der Grid-Ansicht.");

            builder.Delete("Admin.Configuration.Settings.CustomerUser.Privacy.EnableCookieConsent.Hint");

            builder.AddOrUpdate("Enums.RuleScope.Cart.Hint",
                "Rule to grant discounts to the customer or offer shipping and payment methods.",
                "Regel, um dem Kunden Rabatte zu gewähren oder Versand- und Zahlarten anzubieten.");

            builder.AddOrUpdate("Enums.RuleScope.Customer.Hint",
                "Rule to automatically assign customers to customer roles per scheduled task.",
                "Regel, um Kunden automatisch per geplanter Aufgabe Kundengruppen zuzuordnen.");

            builder.AddOrUpdate("Enums.RuleScope.Product.Hint",
                "Rule to automatically assign products to categories per scheduled task.",
                "Regel, um Produkte automatisch per geplanter Aufgabe Warengruppen zuzuordnen.");

            builder.AddOrUpdate("Enums.InvalidLanguageRedirectBehaviour.FallbackToWorkingLanguage",
                "Fallback to working language",
                "Zur aktiven Sprache bzw. Standardsprache umleiten");

            builder.AddOrUpdate("Enums.InvalidLanguageRedirectBehaviour.ReturnHttp404",
                "Return HTTP 404 (page not found) (recommended)",
                "HTTP 404 zurückgeben (Seite nicht gefunden) (empfohlen)");

            builder.AddOrUpdate("Enums.InvalidLanguageRedirectBehaviour.Tolerate", "Tolerate", "Tolerieren");

            builder.Delete("Enums.CookieConsentRequirement.Disabled");
            builder.AddOrUpdate("Enums.CookieConsentRequirement.NeverRequired", "Never required", "Nie erforderlich");

            builder.AddOrUpdate("Admin.System.SystemInfo.AppVersion", "Smartstore version", "Smartstore Version");

            builder.AddOrUpdate("Products.ToFilterAndSort", "Filter & Sort", "Filtern & Sortieren");
            builder.AddOrUpdate("Admin.Common.SaveClose", "Save & close", "Speichern & schließen");
            builder.AddOrUpdate("Admin.Common.SaveExit", "Save & exit", "Speichern & beenden");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.OpenPreviousCombination",
                "Open previous attribute combination",
                "Vorherige Attribut-Kombination öffnen");
            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.OpenNextCombination",
                "Open next attribute combination",
                "Nächste Attribut-Kombination öffnen");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.AddTitle",
                "Add attribute combination",
                "Attribut-Kombination hinzufügen");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.DeliveryTimeIdForEmptyStock",
                "Delivery time displayed when stock is empty",
                "Angezeigte Lieferzeit bei leerem Lager",
                "Delivery time to be displayed when the stock quantity of a product is equal or less 0.",
                "Lieferzeit, die angezeigt wird, wenn der Lagerbestand des Produkts kleiner oder gleich 0 ist.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowCategoryProductNumber",
                "Show number of products next to the categories",
                "Produktanzahl neben den Warengruppen anzeigen");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.SelectAttributes",
                "Set the attributes for the new combination",
                "Bestimmen Sie die Attribute für die neue Kombination");

            builder.AddOrUpdate("Payment.MissingCheckoutState",
                "Missing checkout session state ({0}). Your payment cannot be processed. Please go to back to the shopping cart and checkout again.",
                "Fehlender Checkout-Sitzungsstatus ({0}). Ihre Zahlung kann leider nicht verarbeitet werden. Bitte gehen Sie zurück zum Warenkorb und wiederholen Sie den Bestellvorgang.");

            builder.AddOrUpdate("Account.MyOrders", "My orders", "Meine Bestellungen");

            builder.AddOrUpdate("GiftCardAttribute.For.Physical", "For", "Für");
            builder.AddOrUpdate("GiftCardAttribute.For.Virtual", "For", "Für");
            builder.AddOrUpdate("GiftCardAttribute.From.Physical", "From", "Von");
            builder.AddOrUpdate("GiftCardAttribute.From.Virtual", "From", "Von");

            builder.AddOrUpdate("ShoppingCart.MoveToWishlist", "Move to wishlist", "Auf die Wunschliste");

            builder.AddOrUpdate("Admin.Catalog.Products.CartQuantity",
                "Cart quantity",
                "Bestellmenge");

            builder.AddOrUpdate("Admin.Catalog.Products.CartQuantity.Info",
                "If the number of possible order quantities is less than {0}, a drop-down menu is offered as a control for selecting the order quantity. Otherwise, a numeric input field is generated to allow free entry of the order quantity. The upper limit can be changed in the <a href='{1}' class='alert-link'>shopping cart settings</a>.",
                "Wenn die Anzahl der möglichen Bestellmengen kleiner als {0} ist, wird ein Dropdown-Menü als Steuerelement für die Bestellmengenauswahl angeboten. Ansonsten wird ein numerisches Eingabefeld generiert, das eine freie Erfassung der Bestellmenge ermöglicht. Die Obergrenze kann in den <a href='{1}' class='alert-link'>Warenkorb-Einstellungen</a> geändert werden.");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.MaxQuantityInputDropdownItems",
                "Upper limit for order quantity selection via drop-down menu",
                "Obergrenze für die Bestellmengenauswahl via Dropdown-Menü",
                "Specifies the upper limit of possible order quantities up to which a drop-down menu for entering the order quantity is to be offered. If the number is greater, a numeric input field is used.",
                "Legt die Obergrenze möglicher Bestellmengen fest, bis zu der ein Dropdown-Menü zur Eingabe der Bestellmenge angeboten werden soll. Ist die Anzahl größer, wird ein numerisches Eingabefeld als Steuerelement verwendet.");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.HideQuantityControl",
                "Hide quantity selection on product pages",
                "Mengenauswahl auf Produktseiten ausblenden",
                "Hides the quantity selection control on product pages. If enabled, 'Minimum cart quantity' determines the quantity added to the cart.",
                "Blendet das Mengenauswahl-Steuerlement auf Produktseiten aus. 'Mindestbestellmenge' bestimmt i.d.F. die dem Warenkorb hinzugefügte Menge.");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.AllowedQuantities",
                "Custom quantities",
                "Benutzerdefinierte Mengenliste",
                "A comma-separated list of allowed order quantities for this product. Customers in this case select an order quantity from a drop-down menu instead of making a free entry. If this field is populated, the min/max/step settings will be disabled.",
                "Eine kommagetrennte Liste mit erlaubten Bestellmengen für dieses Produkt. Kunden wählen i.d.F. eine Bestellmenge aus einem Dropdown-Menü aus, anstatt eine freie Eingabe zu tätigen. Wenn dieses Feld befüllt ist, werden Min/Max/Schritt außer Kraft gesetzt.");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.QuantityStep",
                "Quantity step",
                "Mengenschritt",
                "The order quantity is limited to a multiple of this value.",
                "Die Bestellmenge ist auf ein Vielfaches dieses Wertes beschränkt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.ExtraRobotsLines",
                "Extra entries for robots.txt",
                "Extra Einträge für robots.txt");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.DisplayAllows",
                "Items for 'Allow'",
                "Einträge für 'Allow'");
            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.DisplayDisallows",
                "Items for 'Disallow'",
                "Einträge für 'Disallow'");
            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.DisplayAdditionalLines",
                "Additional lines'",
                "Zusätzliche Zeilen");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.RobotsHint",
                "The robots.txt file consists of automatically generated entries, as well as entries for user-defined Allows, Disallows and additional lines. " +
                "Each line of the Allows and Disallows entries is prefixed with the corresponding prefix. " +
                "The entries that are defined as additional lines are appended to the file unchanged.",
                "Die Datei robots.txt besteht aus automatisch generierten Einträgen, sowie aus Einträgen für benutzerdefinierte Allows, Disallows und zusätzlichen Zeilen. " +
                "Dabei wird jeder Zeile der Allows- und Disallows-Einträge das entsprechende Präfix vorangestellt. " +
                "Die Einträge, die als zusätzliche Zeilen hinterlegt sind, werden unverändert an die Datei angehängt.");

            builder.Delete("Account.Navigation");

            builder.AddOrUpdate("PageTitle.Checkout.BillingAddress", "Billing address", "Rechnungsadresse");
            builder.AddOrUpdate("PageTitle.Checkout.ShippingAddress", "Shipping address", "Lieferadresse");
            builder.AddOrUpdate("PageTitle.Checkout.ShippingMethod", "Shipping method", "Versandart");
            builder.AddOrUpdate("PageTitle.Checkout.PaymentMethod", "Payment method", "Zahlart");
            builder.AddOrUpdate("PageTitle.Checkout.Confirm", "Confirm order", "Bestellbestätigung");
            builder.AddOrUpdate("PageTitle.Checkout.Completed", "Thank you!", "Vielen Dank!");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.Privacy.VisitorCookieExpirationDays",
                "Visitor cookies expiration date (in days)",
                "Verfalldatum von Besucher-Cookies (in Tagen)",
                "Specifies the number of days after which guest visitor cookies expire. Default are 365 days.",
                "Legt die Anzahl der Tage fest, nach denen Besucher-Cookies von Gästen verfallen. Standard sind 365 Tage.");

            builder.AddOrUpdate("Admin.Address.Fields.Name.InvalidChars",
                "Please check your input. Numbers and the following characters are not allowed: {0}",
                "Bitte überprüfen Sie Ihre Eingabe. Zahlen und folgende Zeichen sind nicht erlaubt: {0}");

            builder.AddOrUpdate("ShoppingCart.SelectAttribute",
                "Please select <b class=\"fwm\">{0}</b>.",
                "Bitte <b class=\"fwm\">{0}</b> auswählen.");

            builder.AddOrUpdate("ShoppingCart.EnterAttributeValue",
                "Please enter <b class=\"fwm\">{0}</b>.",
                "Bitte <b class=\"fwm\">{0}</b> eingeben.");

            builder.AddOrUpdate("ShoppingCart.UploadAttributeFile",
                "Please upload <b class=\"fwm\">{0}</b>.",
                "Bitte <b class=\"fwm\">{0}</b> hochladen.");

            builder.AddOrUpdate("Admin.System.Maintenance.TreePaths.Title", "Tree paths", "Hierarchie Pfade");

            builder.AddOrUpdate("Admin.System.Maintenance.TreePaths.Hint",
                "Tree paths provide quick access to hierarchically organized data records, such as product categories. " +
                "In very rare cases, gaps can occur here, e.g. due to faulty migrations or imports. " +
                "Problems with missing paths include products appearing in categories to which they are not assigned. " +
                "If you experience such problems in your shop, you can generate the missing paths here.",
                "Hierarchiepfade ermöglichen die performante Abfrage hierarchisch geordneter Datensätze wie z.B. Warengruppen. " +
                "In sehr seltenen Fällen kann es hier zu Lücken kommen, z.B. durch fehlerhafte Migrationen oder Importe. " +
                "Probleme mit fehlenden Pfaden äußern sich u.a. darin, dass Produkte in Warengruppen angezeigt werden, denen sie nicht zugeordnet sind. " +
                "Sollten Sie solche Fehler in Ihrem Shop feststellen, können Sie die fehlenden Pfade hier nachgenerieren lassen.");

            builder.AddOrUpdate("Admin.System.Maintenance.TreePaths.Rebuild", "Check & repair", "Prüfen & reparieren");
            builder.AddOrUpdate("Admin.System.Maintenance.TreePaths.PathCount",
                "The task was completed successfully. {0} new paths were generated.",
                "Die Aufgabe wurde erfolgreich abgeschlossen. Es wurden {0} neue Pfade generiert.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StoreLastUserAgent",
                "Store last user agent",
                "Zuletzt verwendeten User-Agent speichern",
                "When enabled, the last user agent of customers will be stored.",
                "Legt fest, ob der zuletzt verwendete User-Agent im Kundendatensatz gespeichert werden soll.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StoreLastDeviceFamily",
                "Store last device family",
                "Letzte Gerätefamilie speichern",
                "When enabled, the last device family of customers (ze.g. Windows, Android, iPad etc.) will be stored.",
                "Legt fest, ob die zuletzt verwendete Gerätefamilie (z.B. Windows, Android, iPad etc.) im Kundendatensatz gespeichert werden soll.");

            builder.AddOrUpdate("Account.CustomerSince", "Customer since {0}", "Kunde seit {0}");

            builder.AddOrUpdate("Admin.Packaging.Dialog.PluginInfo",
                "Choose a plugin package file (Smartstore.Module.*.zip) to upload to your server. The package will be automatically extracted and displayed after clicking <i>Reload list of plugins</i>. If an older version of the plugin already exists, it will be backed up for you.",
                "Wählen Sie die Plugin Paket-Datei (Smartstore.Module.*.zip), die Sie auf den Server hochladen möchten. Das Paket wird automatisch entpackt und mit einem Klick auf <i>Plugin-Liste erneut laden</i> angezeigt. Wenn eine ältere Version des Plugins bereits existiert, wird eine Sicherungskopie davon erstellt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.SEOSettings.RestartInfo",
                "Changing link options will take effect only after restarting the application. Also, the XML sitemap should be regenerated to reflect the changes.",
                "Das Ändern von Link-Optionen wird erst nach einem Neustart der Anwendung wirksam. Außerdem sollte die XML Sitemap neu generiert werden.");

            builder.AddOrUpdate("Admin.System.Maintenance.DeleteGuests.StartDate.Hint",
                "The start date of the search. If no date is specified, everything before the end date is deleted.",
                "Das Anfangsdatum der Suche. Wird kein Datum angegeben, wird alles bis zum Enddatum gelöscht.");

            builder.AddOrUpdate("Admin.System.Maintenance.DeleteExportedFiles.StartDate.Hint",
                "The start date of the search. If no date is specified, everything before the end date is deleted.",
                "Das Anfangsdatum der Suche. Wird kein Datum angegeben, wird alles bis zum Enddatum gelöscht.");

            builder.AddOrUpdate("Admin.System.Maintenance.DeleteGuests.EndDate.Hint",
                "The end date of the search. If no date is specified, everything beginning on the start date is deleted.",
                "Das Enddatum der Suche. Wenn kein Datum angegeben wird, wird alles ab dem Anfangsdatum gelöscht.");

            builder.AddOrUpdate("Admin.System.Maintenance.DeleteExportedFiles.EndDate.Hint",
                "The end date of the search. If no date is specified, everything beginning on the start date is deleted.",
                "Das Enddatum der Suche. Wenn kein Datum angegeben wird, wird alles ab dem Anfangsdatum gelöscht.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.SameServerNote",
                "Backing up and restoring databases is only possible if the database server (e.g. MS SQL Server or MySQL) and the physical location of the store installation are on the same server.",
                "Sicherungen und Wiederherstellungen von Datenbanken sind nur möglich, wenn sich der Datenbankserver (z.B. MS SQL Server oder MySQL) und der physikalische Speicherort der Shop-Installation auf dem gleichen Server befinden.");

            builder.AddOrUpdate("Admin.System.Maintenance.StartDateMustBeBeforeEndDate",
                "The start date must not be after the end date.",
                "Das Anfangsdatum darf nicht nach dem Enddatum liegen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.SocialSettings.TwitterLink",
                "X (Twitter) link",
                "X (Twitter) Link");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.SocialSettings.TwitterSite",
                "X (Twitter) Username",
                "Benutzername auf X (Twitter)");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.SocialSettings.TwitterSite.Hint",
                "X (Twitter) username that gets displayed on X (Twitter) cards when a product, category and manufacturer page is shared on X (Twitter). Starts with a '@'.",
                "Benutzername auf X (Twitter), der auf Karten von X (Twitter) angezeigt wird, wenn ein Produkt, eine Kategorie oder eine Herstellerseite auf X (Twitter) geteilt wird. Beginnt mit einem '@'.");

            builder.Delete("Admin.Configuration.Settings.Catalog.ShowShareButton");
            builder.Delete("Admin.Configuration.Settings.Catalog.ShowShareButton.Hint");
            builder.Delete("Admin.Configuration.Settings.Catalog.PageShareCode");
            builder.Delete("Admin.Configuration.Settings.Catalog.PageShareCode.Hint");

            builder.AddOrUpdate("Common.DontAskAgain", "Don't ask again", "Nicht mehr fragen");
            builder.AddOrUpdate("Common.DontShowAgain", "Don't show again", "Nicht mehr anzeigen");

            builder.AddOrUpdate("Admin.Catalog.Categories.AutomatedAssignmentRules.Hint",
                "Products are automatically assigned to this category by scheduled task if they fulfill one of the selected rules and this rule is active.",
                "Produkte werden automatisch per geplanter Aufgabe dieser Warengruppe zugeordnet, wenn sie eine der gewählten Regeln erfüllen und diese Regel aktiv ist.");

            builder.AddOrUpdate("Admin.Configuration.Settings.AllSettings", "All settings", "Alle Einstellungen");
        }

        /// <summary>
        /// Migrates obsolete payment description setting (xyzSettings.DescriptionText) of offline payment methods.
        /// </summary>
        private static async Task MigrateOfflinePaymentDescriptions(SmartDbContext db, CancellationToken cancelToken)
        {
            var names = new Dictionary<string, string>
            {
                { "Payments.CashOnDelivery", "CashOnDeliveryPaymentSettings.DescriptionText" },
                { "Payments.DirectDebit", "DirectDebitPaymentSettings.DescriptionText" },
                { "Payments.Invoice", "InvoicePaymentSettings.DescriptionText" },
                { "Payments.Manual", "ManualPaymentSettings.DescriptionText" },
                { "Payments.PurchaseOrderNumber", "PurchaseOrderNumberPaymentSettings.DescriptionText" },
                { "Payments.PayInStore", "PayInStorePaymentSettings.DescriptionText" },
                { "Payments.Prepayment", "PrepaymentPaymentSettings.DescriptionText" }
            };

            var settingNames = names.Values.ToList();
            var descriptionSettings = (await db.Settings
                .AsNoTracking()
                .Where(x => settingNames.Contains(x.Name) && x.StoreId == 0)
                .ToListAsync(cancelToken))
                .ToDictionarySafe(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
            if (descriptionSettings.Count == 0)
            {
                return;
            }

            var masterLanguageId = await db.Languages
                .Where(x => x.Published)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancelToken);
            if (masterLanguageId == 0)
            {
                return;
            }

            var systemNames = names.Keys.ToArray();
            var paymentMethods = (await db.PaymentMethods
                .Where(x => systemNames.Contains(x.PaymentMethodSystemName))
                .ToListAsync(cancelToken))
                .ToDictionarySafe(x => x.PaymentMethodSystemName, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var pair in names)
            {
                var systemName = pair.Key;
                var settingName = pair.Value;

                if (paymentMethods.TryGetValue(systemName, out var pm) && pm != null && pm.FullDescription.HasValue())
                {
                    // Nothing to do. Do not overwrite.
                    continue;
                }

                if (descriptionSettings.TryGetValue(settingName, out var setting) && setting.Value.HasValue())
                {
                    var description = setting.Value;
                    if (description.StartsWithNoCase("@Plugins.Payment"))
                    {
                        var resourceName = description[1..];

                        description = await db.LocaleStringResources
                            .Where(x => x.LanguageId == masterLanguageId && x.ResourceName == resourceName)
                            .Select(x => x.ResourceValue)
                            .FirstOrDefaultAsync(cancelToken);
                    }

                    if (description.HasValue())
                    {
                        // Ignore PaymentMethod's localized properties. The old xyzSettings.DescriptionText had no localization at all.
                        if (pm == null)
                        {
                            db.PaymentMethods.Add(new PaymentMethod
                            {
                                PaymentMethodSystemName = systemName,
                                FullDescription = description,
                            });
                        }
                        else
                        {
                            pm.FullDescription = description;
                        }
                    }
                }
            }

            await db.SaveChangesAsync(cancelToken);

            // Delete obsolete offline payment settings.
            settingNames.AddRange(new[]
            {
                "CashOnDeliveryPaymentSettings.ThumbnailPictureId",
                "DirectDebitPaymentSettings.ThumbnailPictureId",
                "InvoicePaymentSettings.ThumbnailPictureId",
                "ManualPaymentSettings.ThumbnailPictureId",
                "PurchaseOrderNumberPaymentSettings.ThumbnailPictureId",
                "PayInStorePaymentSettings.ThumbnailPictureId",
                "PrepaymentPaymentSettings.ThumbnailPictureId"
            });

            await db.Settings
                .Where(x => settingNames.Contains(x.Name))
                .ExecuteDeleteAsync(cancelToken);
        }

        /// <summary>
        /// Migrates the import profile column mapping of localized properties (if any) from language SEO code to language culture for most languages (see issue #531).
        /// </summary>
        private static async Task MigrateImportProfileColumnMappings(SmartDbContext db, CancellationToken cancelToken)
        {
            try
            {
                var profiles = await db.ImportProfiles
                    .Where(x => x.ColumnMapping.Length > 3)
                    .ToListAsync(cancelToken);

                if (profiles.Count == 0)
                {
                    return;
                }

                var mapConverter = new ColumnMapConverter();
                var allLanguages = await db.Languages.AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(cancelToken);
                var fallbackCultures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "aa", "ar-SA" },
                    { "cs", "cs-CZ" },
                    { "da", "da-DK" },
                    { "el", "el-GR" },
                    { "en", "en-US" },
                    { "et", "et-EE" },
                    { "he", "he-IL" },
                    { "hi", "hi-IN" },
                    { "ja", "ja-JP" },
                    { "ko", "ko-KR" },
                    { "sl", "sl-SI" },
                    { "sq", "sq-AL" },
                    { "sv", "sv-SE" },
                    { "uk", "uk-UA" },
                    { "vi", "vi-VN" },
                    { "zh", "zh-CN" },
                };

                foreach (var profile in profiles)
                {
                    var storedMap = mapConverter.ConvertFrom<ColumnMap>(profile.ColumnMapping);
                    if (storedMap == null)
                        continue;

                    var map = new ColumnMap();
                    var update = false;

                    foreach (var mapping in storedMap.Mappings.Select(x => x.Value))
                    {
                        var mappedName = mapping.MappedName;

                        if (MapColumnName(mappedName, out string name2, out string index2))
                        {
                            mappedName = $"{name2}[{index2}]";
                            update = true;
                        }

                        if (MapColumnName(mapping.SourceName, out string name, out string index))
                        {
                            update = true;
                        }

                        map.AddMapping(name, index, mappedName, mapping.Default);
                    }

                    if (update)
                    {
                        profile.ColumnMapping = mapConverter.ConvertTo(map);
                    }
                }

                await db.SaveChangesAsync(cancelToken);

                bool MapColumnName(string sourceName, out string name, out string index)
                {
                    ColumnMap.ParseSourceName(sourceName, out name, out index);

                    if (name.HasValue() && index.HasValue() && index.Length == 2)
                    {
                        var seoCode = index;
                        var newCulture = $"{seoCode.ToLowerInvariant()}-{seoCode.ToUpperInvariant()}";

                        var language =
                            allLanguages.FirstOrDefault(x => x.LanguageCulture.EqualsNoCase(newCulture)) ??
                            allLanguages.FirstOrDefault(x => x.UniqueSeoCode.EqualsNoCase(seoCode));

                        if (language != null)
                        {
                            index = language.LanguageCulture;
                            return true;
                        }

                        if (fallbackCultures.TryGetValue(index, out newCulture))
                        {
                            index = newCulture;
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch
            {
            }
        }
    }
}
