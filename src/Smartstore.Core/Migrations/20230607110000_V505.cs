using FluentMigrator;
using Smartstore.Core.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-06-07 11:00:00", "V505")]
    internal class V505 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            await MigrateSettingsAsync(context, cancelToken);
        }

        public async Task MigrateSettingsAsync(SmartDbContext db, CancellationToken cancelToken = default)
        {
            var enableCookieConsentSettings = await db.Settings
                .Where(x => x.Name == "PrivacySettings.EnableCookieConsent")
                .ToListAsync(cancelToken);

            if (enableCookieConsentSettings.Count > 0)
            {
                foreach (var setting in enableCookieConsentSettings)
                {
                    db.Settings.Add(new Setting
                    {
                        Name = "PrivacySettings.CookieConsentRequirement",
                        Value = setting.Value.ToBool() ? CookieConsentRequirement.RequiredInEUCountriesOnly.ToString() : CookieConsentRequirement.NeverRequired.ToString(),
                        StoreId = setting.StoreId
                    });
                }
            }

            await db.SaveChangesAsync(cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Account.Fields.Password", "Password", "Passwort");
            builder.AddOrUpdate("Account.Fields.PasswordSecurity", "Password security", "Passwortsicherheit");

            builder.AddOrUpdate("Account.Register.Result.AlreadyRegistered", "You are already registered.", "Sie sind bereits registriert.");
            builder.AddOrUpdate("Admin.Common.Cleanup", "Cleanup", "Aufräumen");

            builder.AddOrUpdate("Admin.System.QueuedEmails.DeleteAll.Confirm",
                "Are you sure you want to delete all sent or undeliverable emails?",
                "Sind Sie sicher, dass alle gesendeten oder unzustellbaren E-Mails gelöscht werden sollen?");

            builder.AddOrUpdate("Admin.ContentManagement.Menus.Item.InvalidTargetLink",
                "Unknown or invalid target \"{0}\" at menu link \"{1}\".",
                "Unbekanntes oder ungültiges Ziel \"{0}\" bei Menü-Link \"{1}\".");

            builder.AddOrUpdate("Account.Fields.StreetAddress2", "Street address 2", "Adresszusatz");
            builder.AddOrUpdate("Address.Fields.Address2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("Address.Fields.Address2.Required", "Address 2 is required.", "Adresszusatz wird benötigt");
            builder.AddOrUpdate("Admin.Address.Fields.Address2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("Admin.Address.Fields.Address2.Hint", "Enter address 2", "Adresszusatz bzw. zweite Adresszeile");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Enabled",
                "'Street address addition' enabled",
                "\"Adresszusatz\" aktiv");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Enabled.Hint",
                "Defines whether the input of 'street address addition' is enabled.",
                "Legt fest, ob das Eingabefeld \"Adresszusatz\" aktiviert ist");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Required",
                "'Street address addition' required", "\"Adresszusatz\" ist erforderlich");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Required.Hint",
                "Defines whether 'street address addition' is required.",
                "Legt fest, ob die Eingabe von \"Adresszusatz\" erforderlich ist.");
            builder.AddOrUpdate("Admin.Customers.Customers.Fields.StreetAddress2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("Admin.Customers.Customers.Fields.StreetAddress2.Hint", "The address 2.", "Adresszusatz");
            builder.AddOrUpdate("Admin.Orders.Address.Address2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("PDFPackagingSlip.Address2", "Address 2: {0}", "Adresszusatz: {0}");

            var generalCommon = "Admin.Configuration.Settings.GeneralCommon";
            var socialSettings = "Admin.Configuration.Settings.GeneralCommon.SocialSettings";

            builder.Delete(
                $"{socialSettings}.FacebookLink.Hint",
                $"{socialSettings}.InstagramLink.Hint",
                $"{socialSettings}.PinterestLink.Hint",
                $"{socialSettings}.TwitterLink.Hint",
                $"{socialSettings}.YoutubeLink.Hint");

            builder.AddOrUpdate($"{socialSettings}.LeaveEmpty",
                "Leave empty to hide the link.",
                "Leer lassen, um den Link auszublenden.");

            builder.AddOrUpdate($"{socialSettings}.FlickrLink", "Flickr Link", "Flickr Link");
            builder.AddOrUpdate($"{socialSettings}.LinkedInLink", "LinkedIn Link", "LinkedIn Link");
            builder.AddOrUpdate($"{socialSettings}.XingLink", "Xing Link", "Xing Link");
            builder.AddOrUpdate($"{socialSettings}.TikTokLink", "TikTok Link", "TikTok Link");
            builder.AddOrUpdate($"{socialSettings}.SnapchatLink", "Snapchat Link", "Snapchat Link");
            builder.AddOrUpdate($"{socialSettings}.VimeoLink", "Vimeo Link", "Vimeo Link");
            builder.AddOrUpdate($"{socialSettings}.TumblrLink", "Tumblr Link", "Tumblr Link");
            builder.AddOrUpdate($"{socialSettings}.ElloLink", "Ello Link", "Ello Link");
            builder.AddOrUpdate($"{socialSettings}.BehanceLink", "Behance Link", "Behance Link");

            builder.AddOrUpdate($"{generalCommon}").Value("en", "General settings");
            builder.AddOrUpdate($"{generalCommon}.SecuritySettings").Value("en", "Security");
            builder.AddOrUpdate($"{generalCommon}.LocalizationSettings").Value("en", "Localization");
            builder.AddOrUpdate($"{generalCommon}.PdfSettings").Value("en", "PDF");

            var seoSettings = $"{generalCommon}.SEOSettings";

            builder.AddOrUpdate($"{seoSettings}", "SEO", "SEO");
            builder.AddOrUpdate($"{seoSettings}.Routing", "Internal links", "Interne Links");
            builder.AddOrUpdate($"{generalCommon}.AppendTrailingSlashToUrls",
                "Append trailing slash to links",
                "Links mit Schrägstrich abschließen",
                "Forces all internal links to end with a trailing slash.",
                "Erzwingt, dass alle internen Links mit einem Schrägstrich abschließen.");
            builder.AddOrUpdate($"{generalCommon}.TrailingSlashRule",
                "Trailing slash mismatch rule",
                "Regel für Nichtübereinstimmung",
                "Rule to apply when an incoming URL does not match the 'Append trailing slash to links' setting.",
                "Regel, die angewendet werden soll, wenn eine eingehende URL nicht mit der Option 'Links mit Schrägstrich abschließen' übereinstimmt.");

            builder.AddOrUpdate($"{seoSettings}.RestartInfo",
                "Changing link options will take effect only after restarting the application. Also, the XML sitemap should be regenerated to reflect the changes.",
                "Das Ändern von Link-Optionen wird erst nach einen Neustart der Anwendung wirksam. Außerdem sollte die XML Sitemap neu generiert werden.");

            builder.AddOrUpdate("Enums.TrailingSlashRule.Allow", "Allow", "Erlauben");
            builder.AddOrUpdate("Enums.TrailingSlashRule.Redirect", "Redirect (recommended)", "Weiterleiten (empfohlen)");
            builder.AddOrUpdate("Enums.TrailingSlashRule.RedirectToHome", "Redirect to home", "Zur Startseite weiterleiten");
            builder.AddOrUpdate("Enums.TrailingSlashRule.Disallow", "Disallow (HTTP 404)", "Nicht zulassen (HTTP 404)");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.LowStockActivity.Hint",
                "Action to be taken when the stock quantity reaches or falls below the minimum stock quantity.",
                "Zu ergreifende Maßnahme, wenn der Lagerbestand den Mindestbestand erreicht oder unterschreitet.");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.MinStockQuantity.Hint",
                "Specifies the minimum stock quantity. If the inventory is tracked and the stock reaches or falls below this value, various actions can be performed (e.g. notification or deactivation of the product).",
                "Legt den Mindestlagerbestand fest. Bei aktivierter Lagerbestandsverwaltung können verschiedene Aktionen ausgeführt werden (z.B. Benachrichtigung oder Deaktivierung des Produktes), sobald der Mindestlagerbestand erreicht oder unterschritten wird.");

            var storeFields = "Admin.Configuration.Stores.Fields";

            builder.AddOrUpdate($"{storeFields}.Url",
                "Shop URL",
                "Shop URL",
                "The root URL of your store, including the base path (if any), e.g. https://mystore.com/",
                "Die Stamm-URL Ihres Shops, einschließlich des Basispfads (falls vorhanden), z.B. https://mystore.com/");

            builder.AddOrUpdate($"{storeFields}.SslEnabled",
                "Require HTTPS",
                "HTTPS erforderlich",
                "Turn this on to enable automatic HTTP to HTTPS redirection, but only if you have a valid SSL certificate installed on your server.",
                "Aktiviert HTTP zu HTTPS Weiterleitung. Stellen Sie sicher, dass ein gültiges SSL-Zertifikat auf dem Server installiert ist, ehe Sie diese Option aktivieren.");

            builder.AddOrUpdate($"{storeFields}.SslPort",
                "HTTPS port",
                "HTTPS Port",
                "Specifies the HTTPS port to append to the host defined in 'Shop URL' for HTTP to HTTPS redirects (but only if 'Shop URL' does not already start with https://).",
                "Legt den HTTPS Port fest, der dem unter 'Shop URL' definierten Host bei HTTP zu HTTPS Weiterleitungen angehangen werden soll (aber nur, wenn 'Shop URL' nicht bereits mit https:// beginnt).");

            builder.Delete(
                $"{storeFields}.SslEnabled",
                $"{storeFields}.SslEnabled.Hint",
                $"{storeFields}.ForceSslForAllPages",
                $"{storeFields}.ForceSslForAllPages.Hint");

            builder.AddOrUpdate("Pager.Previous", "Previous", "Zurück");
            builder.AddOrUpdate("Pager.Next", "Next", "Weiter");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.ProductWithDeliveryTimeInCart",
                "Product with delivery time in cart",
                "Produkt mit Lieferzeit im Warenkorb");

            builder.AddOrUpdate("Admin.DataExchange.Export.Filter.IncludeSubCategories",
                "Include subcategories",
                "Unterwarengruppen einschließen",
                "Specifies whether products from subcategories should also be filtered.",
                "Legt fest, ob Produkte von Unterwarengruppen ebenfalls gefiltert werden sollen.");

            // Typos.
            builder.AddOrUpdate("Admin.DataExchange.Export.Filter.CategoryIds.Hint")
                .Value("en", "Filter by categories.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductsFromSubcategories.Hint")
                .Value("de", "Legt fest, ob Unterwarengruppen in Warengruppen-Detailseiten angezeigt werden sollen.");

            builder.AddOrUpdate("Forum.TotalPosts", "Posts", "Beiträge");

            builder.AddOrUpdate("Admin.Catalog.Products.List.GoDirectlyToSku",
                "Find by SKU, GTIN or MPN",
                "Nach SKU, EAN oder MPN suchen",
                "Opens directly the edit page of the product with the specified SKU, EAN or MPN (manufacturer part number).",
                "Öffnet direkt die Bearbeitungsseite des Produktes mit der angegebenen SKU, GTIN oder MPN (Hersteller-Produktnummer).");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.ManufacturerPartNumber",
                "Manufacturer part number (MPN)",
                "Hersteller-Produktnummer (MPN)",
                "Specifies the manufacturer's part number (MPN).",
                "Legt die Produktnummer des Herstellers (MPN) fest.");

            builder.AddOrUpdate("Admin.Configuration.Settings.EmailAccount.DefaultEmailAccountId",
                "Default email account",
                "Standard E-Mail-Konto",
                "Specifies the default email account.",
                "Legt das Standard E-Mail-Konto fest.");

            builder.Delete(
                "Admin.Configuration.Settings.Catalog.EnableDynamicPriceUpdate",
                "Admin.Configuration.Settings.Catalog.EnableDynamicPriceUpdate.Hint",
                "Admin.Order.NotFound",
                "Admin.AccessDenied");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.Privacy.CookieConsentRequirement",
                "Cookie consent requirement",
                "Cookie-Zustimmung erforderlich",
                "Determines whether and in which regions the cookie manager dialog should be displayed.",
                "Bestimmt, ob und in welchen Regionen der Cookie-Manager-Dialog angezeigt werden soll.");

            builder.AddOrUpdate("Enums.CookieConsentRequirement.NeverRequired", "Never required", "Nie erforderlich");
            builder.AddOrUpdate("Enums.CookieConsentRequirement.RequiredInEUCountriesOnly", "Required in EU countries only (recommended)", "Nur in EU-Ländern erforderlich (empfohlen)");
            builder.AddOrUpdate("Enums.CookieConsentRequirement.DependsOnCountry", "Depends on country configuration", "Abhängig von der Länderkonfiguration");

            builder.Delete("Admin.Configuration.Settings.CustomerUser.Privacy.EnableCookieConsent");

            // INFO: Common.Edit wasn't used on purpose in case someone want's to alter this Resource only for forum
            builder.AddOrUpdate("Forum.EditPost", "Edit", "Bearbeiten");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnEmailProductToFriendPage")
                .Value("de", "Auf der \"Produkt Weitersagen\"-Seite anzeigen");
            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnEmailProductToFriendPage.Hint")
                .Value("de", "Legt fest, ob ein CAPTCHA auf der \"Produkt Weitersagen\"-Seite angezeigt werden soll.");

            builder.AddOrUpdate("Admin.Promotions.Campaigns.Warning",
                "Save the campaign and use the preview button to test it before sending it to many customers.",
                "Speichern Sie die Kampagne und benutzen Sie den Vorschau-Button, um sie zu testen, bevor Sie sie an viele Kunden versenden.");

            builder.AddOrUpdate("Admin.DataGrid.XPerPage",
                "<span class='fwm'>{0}</span><span class='d-none d-sm-inline'> per page</span>",
                "<span class='fwm'>{0}</span><span class='d-none d-sm-inline'> pro Seite</span>");

            builder.AddOrUpdate("Admin.Configuration.Stores.CannotDeleteLastStore",
                "The store cannot be deleted. At least one store is required.",
                "Der Shop kann nicht gelöscht werden. Es ist mindestens ein Shop erforderlich.");

            builder.AddOrUpdate("Admin.Configuration.Stores.CannotDeleteStoreWithWalletPostings",
                "The store cannot be deleted. Postings to a credit account are assigned to it.",
                "Der Shop kann nicht gelöscht werden. Ihm sind Buchungen auf ein Guthabenkonto zugeordnet.");

            builder.AddOrUpdate("Admin.Report.ChangeComparedTo",
                "A change of {0} compared to the period {1} to {2}.",
                "Eine Veränderung um {0} im Vergleich zum Zeitraum {1} bis {2}.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Payment.ProductDetailPaymentMethodSystemNames",
                "Payment method icons on the product detail page",
                "Zahlungsart-Icons auf der Produktdetailseite",
                "Specifies for which payment methods icons informing about the accepted payment methods are displayed on product detail pages.",
                "Bestimmt, für welche Zahlarten Icons zur Information über die akzeptierten Zahlungsarten auf Produktdetailseiten angezeigt werden.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Payment.DisplayPaymentMethodIcons",
                "Show icons on the payment method selection page",
                "Icons auf der Zahlartauswahlseite anzeigen");

            builder.AddOrUpdate("Products.PaymentOptions.Heading",
                "Payment options",
                "Zahlungsmöglichkeiten");

            builder.AddOrUpdate("Products.PaymentOptions.Intro",
                "We accept the following payment methods",
                "Wir akzeptieren folgende Zahlungsarten");

            builder.AddOrUpdate("Admin.DataGrid.FitColumns", "Fit columns", "Spalten anpassen");

            builder.AddOrUpdate("Common.ReCaptchaCheckFailed",
                "A reCAPTCHA check failed with the error {0}.",
                "Eine reCAPTCHA-Prüfung ist fehlgeschlagen. Grund: {0}.");

            builder.AddOrUpdate("Order.CannotCapture",
                "The payment of the order could not be captured.",
                "Die Zahlung der Bestellung konnte nicht gebucht werden.");

            builder.AddOrUpdate("Order.CannotVoid",
                "The payment of the order could not be voided.",
                "Die Zahlung der Bestellung konnte nicht storniert werden.");
        }
    }
}
