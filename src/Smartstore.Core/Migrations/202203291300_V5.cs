using FluentMigrator;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Configuration;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Search.Indexing;
using Smartstore.Core.Seo;
using Smartstore.Data.Migrations;
using Smartstore.Utilities;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-03-29 13:00:00", "V5")]
    internal class V5 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            await MigrateEnumResources(context, cancelToken);
            await RemoveTelerikResources(context, cancelToken);
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        private static async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateSettingsAsync(builder =>
            {
                builder.Delete(
                    "CustomerSettings.MinDigitsInPassword",
                    "CustomerSettings.MinSpecialCharsInPassword",
                    "CustomerSettings.MinSpecialCharsInPassword",
                    "CustomerSettings.MinUppercaseCharsInPassword");

                builder.Add(TypeHelper.NameOf<PerformanceSettings>(x => x.UseResponseCompression, true), "False");
            });

            // Remark: In classic code, we always made the mistake to query for FirstOrDefault and thus ignoring multistore settings.
            var settings = context.Set<Setting>();

            var invalidRegisterCustomerRoleIds = await settings.Where(x => x.Name == "CustomerSettings.RegisterCustomerRoleId" && x.Value == "0").ToListAsync(cancelToken);
            if (invalidRegisterCustomerRoleIds.Count > 0)
            {
                settings.RemoveRange(invalidRegisterCustomerRoleIds);
            }

            var gaWidgetZone = await settings.Where(x => x.Name == "GoogleAnalyticsSettings.WidgetZone").ToListAsync(cancellationToken: cancelToken);
            if (gaWidgetZone.Any())
            {
                gaWidgetZone.Each(x => x.Value = x.Value == "head_html_tag" ? "head" : "end");
            }

            var activeWidgetZone = await settings.Where(x => x.Name == "WidgetSettings.ActiveWidgetSystemNames").ToListAsync(cancellationToken: cancelToken);
            if (activeWidgetZone.Any())
            {
                activeWidgetZone.Each(x => x.Value = x.Value.Replace("SmartStore.GoogleAnalytics", "Smartstore.Google.Analytics"));
            }

            var hasPrimaryCurrency = await settings.AnyAsync(x => x.Name == "CurrencySettings.PrimaryCurrencyId" && x.StoreId == 0, cancelToken);
            var hasExchangeCurrency = await settings.AnyAsync(x => x.Name == "CurrencySettings.PrimaryExchangeCurrencyId" && x.StoreId == 0, cancelToken);

            if (!hasPrimaryCurrency || !hasExchangeCurrency)
            {
                var store = await context.Stores
                    .AsNoTracking()
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .FirstOrDefaultAsync(cancelToken);

                if (store != null)
                {
                    if (!hasPrimaryCurrency)
                    {
                        settings.Add(new Setting { Name = "CurrencySettings.PrimaryCurrencyId", Value = store.DefaultCurrencyId.ToString() });
                    }
                    if (!hasExchangeCurrency)
                    {
                        settings.Add(new Setting { Name = "CurrencySettings.PrimaryExchangeCurrencyId", Value = store.PrimaryExchangeRateCurrencyId.ToString() });
                    }
                }
            }

            await context.SaveChangesAsync(cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            #region General

            builder.AddOrUpdate("Admin.NewsFeed.Title", "Newsfeed", "Aktuelles");
            builder.AddOrUpdate("Admin.NewsFeed.ServerDown",
                "Unfortunately the connection to our server could not be established. Please check your internet connection.",
                "Leider konnte keine Verbindung zu unserem Server aufgebaut werden. Bitte prüfen Sie Ihre Internetverbindung.");

            builder.Delete("Admin.Marketplace.News");
            builder.Delete("Admin.Marketplace.ComingSoon");
            builder.Delete("Admin.Marketplace.Visit");

            builder.AddOrUpdate("Admin.Catalog.Products.Orders.NoOrdersAvailable",
                "There are no orders for this product yet.",
                "Für dieses Produkt existieren noch keine Bestellungen.");

            builder.AddOrUpdate("Admin.Orders.List.NoOrdersAvailable",
                "There are no orders for this customer yet.",
                "Für diesen Kunden existieren noch keine Bestellungen.");

            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryStoreCurrencyId");
            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryStoreCurrencyId.Hint");
            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryExchangeRateCurrencyId");
            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryExchangeRateCurrencyId.Hint");

            builder.AddOrUpdate("Admin.Configuration.Stores.Fields.DefaultCurrencyId",
                "Default currency",
                "Standardwährung",
                "Sets the currency that is preselected for this shop in the frontend.",
                "Legt die im Frontend vorausgewählte Währung für diesen Shop fest.");

            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.CycleInfo", "Interval", "Interval");
            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.CyclesRemaining", "Remaining", "Verbleibend");

            builder.AddOrUpdate("Admin.Tax.Categories.NoDuplicatesAllowed",
                "A tax category with this name already exists. Please choose another name.",
                "Eine Steuerklasse mit diesem Namen existiert bereits. Bitte wählen Sie einen anderen Namen.");

            builder.AddOrUpdate("Admin.Customers.Customers.List.SearchCustomerNumber", "Customer number", "Kundennummer");

            builder.AddOrUpdate("Admin.Configuration.EmailAccounts.CannotDeleteDefaultAccount",
                "The default email account \"{0}\" cannot be deleted. Set a different default email account first.",
                "Das Standard-Email-Konto \"{0}\" kann nicht gelöscht werden. Legen Sie zunächst ein anderes Standard-Email-Konto fest.");

            builder.AddOrUpdate("Admin.Configuration.EmailAccounts.CannotDeleteLastAccount",
                "The email account \"{0}\" cannot be deleted. At least one email account is required.",
                "Das E-Mail-Konto \"{0}\" kann nicht gelöscht werden. Es wird mindestens ein E-Mail-Konto benötigt.");

            builder.AddOrUpdate("ExternalAuthentication.ConfigError",
                "There is a problem with the selected login method. Please choose another one or notify the store owner.",
                "Es liegt ein Problem mit der gewählten Login-Methode vor. Bitte wählen Sie eine andere Methode oder benachrichtigen Sie den Shop-Betreiber.");

            builder.Delete("Admin.Customers.Customers.RewardPoints.Fields.AddRewardPointsMessage");
            builder.Delete("Admin.Customers.Customers.RewardPoints.Fields.AddRewardPointsValue");
            builder.Delete("Admin.Customers.Customers.RewardPoints.Fields.AddRewardPointsMessage.Hint");
            builder.Delete("Admin.Customers.Customers.RewardPoints.Fields.AddRewardPointsValue.Hint");

            builder.Delete("Admin.Configuration.Settings.NoneWithThatId");
            builder.Delete("Admin.Configuration.Settings.AllSettings.Description");

            builder.AddOrUpdate("Common.SetDefault", "Set as default", "Als Standard festlegen");

            builder.AddOrUpdate("Admin.Configuration.Currencies.ApplyRate.Error",
                "An error occurred when applying the rate.",
                "Bei der Aktualisierung der Rate ist ein Fehler aufgetreten.");

            builder.AddOrUpdate("Admin.Configuration.QuantityUnit.CannotDeleteAssignedProducts",
                "The quantity unit \"{0}\" cannot be deleted. It has associated products or product variants.",
                "Die Verpackungseinheit \"{0}\" kann nicht gelöscht werden. Ihr sind Produkte oder Produktvarianten zugeordnet.");

            builder.AddOrUpdate("Admin.Configuration.QuantityUnit.CannotDeleteDefaultQuantityUnit",
                "The default quantity unit \"{0}\" cannot be deleted. Set another standard quantity unit first.",
                "Die Standard-Verpackungseinheit \"{0}\" kann nicht gelöscht werden. Bestimmen Sie zuvor eine andere Standard-Verpackungseinheit.");

            builder.AddOrUpdate("Admin.Configuration.DeliveryTimes.CannotDeleteAssignedProducts",
                "The delivery time \"{0}\" cannot be deleted. It has associated products or product variants.",
                "Die Lieferzeit \"{0}\" kann nicht gelöscht werden. Ihr sind Produkte oder Produktvarianten zugeordnet.");

            builder.AddOrUpdate("Admin.Configuration.DeliveryTimes.CannotDeleteDefaultDeliveryTime",
                "The default delivery time \"{0}\" cannot be deleted. Set another standard delivery time first.",
                "Die Standard-Lieferzeit \"{0}\" kann nicht gelöscht werden. Bestimmen Sie zuvor eine andere Standard-Lieferzeit.");

            builder.AddOrUpdate("Admin.ContentManagement.Menus.CannotBeDeleted",
                "The menu \"{0}\" is required by your shop and cannot be deleted.",
                "Das Menü \"{0}\" wird von Ihrem Shop benötigt und kann nicht gelöscht werden.");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.QuantityUnit",
                "Quantity unit",
                "Verpackungseinheit",
                "Specifies the quantity unit.",
                "Legt die Verpackungseinheit fest.");

            builder.AddOrUpdate("Admin.Configuration.Entity.Updated",
                "The entity has been successfully updated.",
                "Die Entität wurde erfolgreich aktualisiert.");

            builder.AddOrUpdate("Admin.Configuration.Entity.Added",
                "The entity has been successfully added.",
                "Die Entität wurde erfolgreich hinzugefügt.");

            builder.AddOrUpdate("Common.Exit", "Exit", "Beenden");
            builder.AddOrUpdate("Common.Empty", "Empty", "Leer");
            builder.AddOrUpdate("Admin.Common.SaveChanges", "Save changes", "Änderungen speichern");
            builder.AddOrUpdate("Admin.Common.EnvironmentVariables", "Environment variables", "Umgebungsvariablen");
            builder.AddOrUpdate("Admin.System.Log.ClearLog.Confirm", "Are you sure that all log entries should be deleted?", "Sind Sie sicher, dass alle Log-Einträge gelöscht werden sollen?");
            builder.AddOrUpdate("Admin.System.QueuedEmails.DeleteAll.Confirm", "Are you sure that all emails should be deleted?", "Sind Sie sicher, dass alle Emails gelöscht werden sollen?");

            builder.AddOrUpdate("Admin.Catalog.Products.Categories.NoDuplicatesAllowed",
                "This category has already been assigned to the product.",
                "Diese Warengruppe wurde dem Produkt bereits zugeordnet.");

            builder.AddOrUpdate("Admin.Catalog.Products.Manufacturers.NoDuplicatesAllowed",
                "This manufacturer has already been assigned to the product.",
                "Dieser Hersteller wurde dem Produkt bereits zugeordnet.");

            builder.AddOrUpdate("Admin.Catalog.Categories.Products.NoDuplicatesAllowed",
                "This product has already been assigned to the category.",
                "Dieses Produkt wurde der Warengruppe bereits zugeordnet.");

            builder.AddOrUpdate("BackInStockSubscriptions.Subscribed",
                "You will be notified when this product is available again.",
                "Sie erhalten eine Benachrichtigung, wenn dieses Produkt wieder lieferbar ist.");

            builder.AddOrUpdate("BackInStockSubscriptions.Unsubscribed",
                "The back in stock notification has been cancelled.",
                "Die Benachrichtigung über Produktverfügbarkeit wurde storniert.");

            builder.AddOrUpdate("Admin.Catalog.Categories.NoCategories",
                "No categories found.",
                "Es wurden keine Warengruppen gefunden.");


            builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.List.SearchName",
                "Name",
                "Name",
                "Filters by the attribute name.",
                "Filtert nach dem Attributnamen.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.List.SearchAlias",
                "Alias",
                "Alias",
                "Filters by the URL alias for search filters.",
                "Filtert nach dem URL-Alias für Suchfilter.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.List.SearchAllowFiltering",
                "Allow filtering",
                "Filtern zulassen",
                "Filters attributes by which search results can be filtered.",
                "Filtert Attribute, nach denen Suchergebnisse eingegrenzt werden können.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.List.SearchName",
                "Name",
                "Name",
                "Filters by the attribute name.",
                "Filtert nach dem Attributnamen.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.List.SearchAlias",
                "Alias",
                "Alias",
                "Filters by the URL alias for search filters.",
                "Filtert nach dem URL-Alias für Suchfilter.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.List.SearchAllowFiltering",
                "Allow filtering",
                "Filtern zulassen",
                "Filters attributes by which search results can be filtered.",
                "Filtert Attribute, nach denen Suchergebnisse eingegrenzt werden können.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.List.SearchShowOnProductPage",
                "Show on product page",
                "Auf der Produktseite anzeigen",
                "Filters attributes that are displayed on the product detail page.",
                "Filtert Attribute, die auf der Produktdetailseite angezeigt werden.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.List.Name",
                "Name",
                "Name",
                "Filters by discount name.",
                "Filtert nach dem Rabattnamen.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.List.DiscountType",
                "Discount type",
                "Rabatttyp",
                "Filters by discount type.",
                "Filtert nach dem Rabatttyp.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.List.UsePercentage",
                "Use percentage",
                "Als Prozentwert",
                "Filters by percentage discounts.",
                "Filtert nach prozentualen Rabatten.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.List.RequiresCouponCode",
                "Requires coupon code",
                "Gutscheincode erforderlich",
                "Filters discounts that require a coupon code.",
                "Filtert Rabatte, bei denen ein Gutscheincode erforderlich ist.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.Fields.AppliedToManufacturers.Hint",
                "A list of manufacturers to which the discount is assigned. The assignment can be made on the manufacturer detail page.",
                "Eine Liste von Herstellern, denen der Rabatt zugeordnet ist. Die Zuordnung kann auf der Hersteller-Detailseite vorgenommen werden.");

            builder.AddOrUpdate("Admin.GiftCards.RecipientEmailInvalid",
                "The recipient email is invalid.",
                "Die E-Mail Adresse des Empfängers ist ungültig.");

            builder.AddOrUpdate("Admin.GiftCards.SenderEmailInvalid",
                "The sender email is invalid.",
                "Die E-Mail Adresse des Absenders ist ungültig.");

            builder.AddOrUpdate("Admin.Common.ViewObject", "View (#{0})", "Ansicht (#{0})");
            builder.AddOrUpdate("Admin.Common.FileName", "File name", "Dateiname");
            builder.AddOrUpdate("Admin.Common.FileSize", "File size", "Dateigröße");
            builder.AddOrUpdate("Admin.Common.Print", "Print", "Drucken");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup", "Database backups", "Datenbanksicherungen");
            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.Download", "Download database backup", "Datenbanksicherung herunterladen");
            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.MatchesCurrentVersion", "Current version", "Aktuelle Version");
            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.Create", "Create backup", "Sicherung erstellen");
            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.Upload", "Upload backup", "Sicherung hochladen");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.BackupNotSupported",
                "The database backup cannot be created because the data provider '{0}' does not support it.",
                "Die Datenbanksicherung kann nicht erstellt werden, da der Daten-Provider '{0}' dies nicht unterstützt.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.RestoreNotSupported",
                "The database cannot be restored because the data provider '{0}' does not support this.",
                "Die Datenbank kann nicht wiederhergestellt werden, da der Daten-Provider '{0}' dies nicht unterstützt.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.BackupCreated",
                "The database backup was successfully created.",
                "Das Datenbank wurde erfolgreich gesichert.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.DatabaseRestored",
                "The database was successfully restored.",
                "Die Datenbank wurde erfolgreich wiederhergestellt.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.Restore",
                "Restore",
                "Wiederherstellen",
                "Restore the database from this backup file.",
                "Datenbank aus dieser Sicherungsdatei (Backup) wiederherstellen.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.RestoreVersionWarning",
                "The backup was created with a different Smartstore version. Restoring it may cause unpredictable issues. Do you still want to proceed?",
                "Die Sicherung wurde mit einer anderen Smartstore Version erstellt. Eine Wiederherstellung kann zu unvorhersehbaren Problemen führen. Möchten Sie trotzdem fortfahren?");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.InvalidBackup",
                "The file \"{0}\" is not a valid database backup. The file name must have the format [database name]-[version]-[timestamp].",
                "Bei der Datei \"{0}\" handelt es sich um keine gültige Datenbanksicherung. Der Dateiname muss das Format [Datenbankname]-[Version]-[Zeitstempel] haben.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.BackupUploaded",
                "The database backup was successfully uploaded.",
                "Die Datenbanksicherung wurde erfolgreich hochgeladen.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.BackupUploadNote",
                "Uploading a database backup can take several minutes depending on the file size.",
                "Der Upload einer Datenbanksicherung kann je nach Dateigröße mehrere Minuten in Anspruch nehmen.");

            builder.AddOrUpdate("Admin.System.Maintenance.DeletedExportFilesAndFolders",
                "{0} export files and {1} export folders have been deleted.",
                "Es wurden {0} Exportdateien und {1} Exportordner gelöscht.");

            builder.AddOrUpdate("Admin.OrderNotice.RefundAmountError",
                "The amount to be refunded must be greater than 0.",
                "Der zu erstattende Betrag muss größer 0 sein.");

            builder.AddOrUpdate("Admin.Orders.Products.NotDownloadable",
                "The product is not downloadable.",
                "Das Produkt kann nicht heruntergeladen werden.");

            builder.AddOrUpdate("Admin.Orders.ProcessWithOrder", "Continue with order {0}?", "Mit Auftrag {0} fortfahren?");

            builder.AddOrUpdate(
                "Admin.Orders.OrderItem.CannotDeleteAssociatedGiftCards",
                "The order item cannot be deleted because gift cards are assigned to it. Please delete the gift cards first.",
                "Die Auftragsposition kann nicht gelöscht werden, weil ihr Geschenkgutscheine zugeordnet sind. Bitte löschen Sie zunächst die Geschenkgutscheine.");

            builder.AddOrUpdate("Admin.RecurringPayments.List.CustomerEmail",
                "Customer email address",
                "Kunden-E-Mail",
                "Filter list by customer email.",
                "Liste nach der Kunden-E-Mail filtern.");

            builder.AddOrUpdate("Admin.RecurringPayments.List.CustomerName",
                "Customer name",
                "Kundenname",
                "Filter list by customer name.",
                "Liste nach dem Kundennamen filtern.");

            builder.AddOrUpdate("Admin.RecurringPayments.List.InitialOrderNumber",
                "Number of the initial order",
                "Nummer des ursprünglichen Auftrages",
                "Filter the list by the number of the initial order.",
                "Liste nach der Nummer des ursprünglichen Auftrages filtern.");

            builder.AddOrUpdate("Admin.Common.Search.StartDate",
                "Start date",
                "Anfangsdatum",
                "Sets the start date of the search.",
                "Legt das Anfangsdatum der Suche fest.");

            builder.AddOrUpdate("Admin.Common.Search.EndDate",
                "End date",
                "Enddatum",
                "Sets the end date of the search.",
                "Legt das Enddatum der Suche fest.");

            builder.AddOrUpdate("Admin.Configuration.Languages.Resources.List.Name",
                "Resource name",
                "Ressourcenname",
                "Filter list by resource name.",
                "Liste nach dem Ressourcenname filtern.");

            builder.AddOrUpdate("Admin.Configuration.Languages.Resources.List.Value",
                "Resource value",
                "Ressourcenwert",
                "Filter list by resource vvalue.",
                "Liste nach dem Ressourcenwert filtern.");

            builder.AddOrUpdate("Admin.Configuration.Languages.Resources", "Resources", "Ressourcen");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.Fields.HelpfulYesTotal",
                "Helpful",
                "Hilfreich",
                "The number of reviews rated as helpful.",
                "Die Anzahl der als hilfreich eingestuften Bewertungen.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.Fields.HelpfulNoTotal",
                "Not helpful",
                "Nicht hilfreich",
                "The number of reviews rated as not helpful.",
                "Die Anzahl der als nicht hilfreich eingestuften Bewertungen.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.List.Rating",
                "Rating",
                "Bewertung",
                "Filter list by rating.",
                "Liste nach Bewertung filtern.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.List.ProductName",
                "Product name",
                "Produktname",
                "Filter list by product name.",
                "Liste nach dem Produktnamen filtern.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberApprovedReviews",
                "There were {0} product reviews approved.",
                "Es wurden {0} Produkt Rezensionen genehmigt.");

            builder.AddOrUpdate("Admin.Catalog.ProductReviews.NumberDisapprovedReviews",
                "There were {0} product reviews disapproved.",
                "Es wurden {0} Produkt Rezensionen abgelehnt.");

            builder.AddOrUpdate("Admin.Common.InvalidFileName", "Invalid file name.", "Ungültiger Dateiname.");
            builder.AddOrUpdate("Common.UserProfile", "User profile", "Benutzerprofil");

            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchDeliveryTime",
                "Delivery time",
                "Lieferzeit",
                "Filter delivery time.",
                "Lieferzeit eingrenzen.");

            builder.AddOrUpdate("Admin.Catalog.Products.BundleItems.CanBeBundleItemWarning",
                "A bundle, grouped product, download or recurring product cannot be added to the bundle item list.",
                "Ein Bundle, Gruppenprodukt, Abo oder Download kann der Stückliste nicht hinzugefügt werden.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.SetAsPrimaryCurrency",
                "Set as primary currency",
                "Als Leitwährung festlegen");

            builder.AddOrUpdate("Admin.Configuration.Currencies.SetAsPrimaryExchangeCurrency",
                "Set as exchange rate currency",
                "Als Umrechnungswährung festlegen");

            builder.AddOrUpdate("Admin.Configuration.Currencies.CannotDeletePrimaryCurrency",
                "The primary currency \"{0}\" cannot be deleted. Set a different primary currency first.",
                "Die Leitwährung \"{0}\" kann nicht gelöscht werden. Legen Sie zunächst eine andere Leitwährung fest.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.CannotDeleteExchangeCurrency",
                "The exchange rate currency \"{0}\" cannot be deleted. Set a different exchange rate currency first.",
                "Die Umrechnungswährung \"{0}\" kann nicht gelöscht werden. Legen Sie zunächst eine andere Umrechnungswährung fest.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.PublishedCurrencyRequired",
                "At least one currency must be published.",
                "Mindestens eine Währung muss veröffentlicht sein.");

            builder.AddOrUpdate("Admin.Orders.Fields.ID",
                "Order ID",
                "Auftrags-ID",
                "The unique ID for this order.",
                "Die eindeutige ID für diesen Auftrag.");

            builder.AddOrUpdate("Admin.ContentManagement.Topics.Fields.IsSystemTopic",
                "System topic",
                "Systemseite",
                "Topics predefined by the system cannot be deleted.",
                "Vom System vorgegebene Seiten und Inhalte können nicht gelöscht werden.");

            builder.AddOrUpdate("Admin.ContentManagement.Topics.Fields.CookieType",
                "Cookie type",
                "Art des Cookies",
                "Sets whether this widget is displayed according to the customer's settings in the cookie manager. This option should be used if you add a third-party script that sets cookies.",
                "Legt fest, ob dieses Widget in Abhängigkeit zur Kundeneinstellung im Cookie-Manager ausgegeben wird. Diese Option sollte verwendet werden, wenn Sie ein Script für einen Drittanbieter zufügen, das Cookies setzt.");

            builder.AddOrUpdate("Permissions.DisplayName.EditExchangeRate", "Edit exchange rates", "Umrechnungskurse bearbeiten");
            builder.AddOrUpdate("Permissions.DisplayName.ReadAddress", "Read addresses", "Adressen lesen");
            builder.AddOrUpdate("Permissions.DisplayName.CreateAddress", "Create addresses", "Adressen erstellen");
            builder.AddOrUpdate("Permissions.DisplayName.DeleteAddress", "Delete addresses", "Adressen löschen");
            builder.AddOrUpdate("Permissions.DisplayName.CreateDeployment", "Create deployment profile", "Veröffentlichungsprofil erstellen");

            #endregion

            #region Packaging

            builder.AddOrUpdate("Admin.Packaging.Dialog.PluginInfo",
                "Choose a plugin package file (Smartstore.Module.*.zip) to upload to your server. The package will be automatically extracted and installed. If an older version of the plugin already exists, it will be backed up for you.",
                "Wählen Sie die Plugin Paket-Datei (Smartstore.Module.*.zip), die Sie auf den Server hochladen möchten. Das Paket wird autom. extrahiert und installiert. Wenn eine ältere Version des Plugins bereits existiert, wird eine Sicherungskopie davon erstellt.");

            builder.AddOrUpdate("Admin.Packaging.Dialog.ThemeInfo",
                "Choose the theme package file (Smartstore.Theme.*.zip) to upload to your server. The package will be automatically extracted and installed. If an older version of the theme already exists, it will be backed up for you.",
                "Wählen Sie die Theme Paket-Datei (Smartstore.Theme.*.zip), die Sie auf den Server hochladen möchten. Das Paket wird autom. extrahiert und installiert. Wenn eine ältere Version des Themes bereits existiert, wird eine Sicherungskopie davon erstellt.");

            builder.AddOrUpdate("Admin.Packaging.InstallSuccess",
                "Package '{0}' was uploaded and unzipped successfully. Please reload the list.",
                "Paket '{0}' wurde hochgeladen und erfolgreich entpackt. Bitte laden Sie die Liste neu.");

            builder.AddOrUpdate("Admin.Packaging.InstallSuccess.Theme",
                "Theme '{0}' was uploaded and installed successfully. Please reload the list.",
                "Theme '{0}' wurde hochgeladen und erfolgreich installiert. Bitte laden Sie die Liste neu.");

            builder.AddOrUpdate("Admin.Packaging.NotAPackage",
                "Package file is invalid. Please upload a 'Smartstore.*.zip' file.",
                "Paket-Datei ist ungültig. Bitte laden Sie eine 'Smartstore.*.zip' Datei hoch.");

            builder.AddOrUpdate("Admin.Packaging.StreamError",
                "Unable to create ZIP archive from stream.",
                "Stream kann nicht in ein ZIP-Archiv konvertiert werden.");

            builder.AddOrUpdate("Admin.Packaging.NotATheme",
                "Package does not contain a theme.",
                "Paket beinhaltet kein Theme.");

            builder.AddOrUpdate("Admin.Packaging.NotAModule",
                "Package does not contain a plugin.",
                "Paket beinhaltet kein Plugin.");

            builder.AddOrUpdate("Admin.Configuration.Settings.AllSettings.AllTypes", "All types", "Alle Typen");

            #endregion

            #region Identity

            builder.Delete("Account.EmailUsernameErrors.UsernameAlreadyExists");    // Isn't used
            builder.Delete("Account.Register.Errors.UsernameAlreadyExists");        // Now is Identity.Error.DuplicateUserName
            builder.Delete("Account.EmailUsernameErrors.EmailAlreadyExists");       // Isn't used
            builder.Delete("Account.Register.Errors.EmailAlreadyExists");           // Now is Identity.Error.DuplicateEmail

            builder.Delete("Account.ChangePassword.Fields.NewPassword.EnteredPasswordsDoNotMatch");     // One resource is enough for this
            builder.Delete("Account.Fields.Password.EnteredPasswordsDoNotMatch");                       // Now is Identity.Error.PasswordMismatch
            builder.Delete("Account.PasswordRecovery.NewPassword.EnteredPasswordsDoNotMatch");          // One resource is enough for this

            builder.Delete("Account.Fields.Password.Digits");                       // Old password validation
            builder.Delete("Account.Fields.Password.SpecialChars");                 // Old password validation
            builder.Delete("Account.Fields.Password.UppercaseChars");               // Old password validation
            builder.Delete("Account.Fields.Password.MustContainChars");             // Old password validation

            builder.Delete("Account.ChangePassword.Errors.EmailIsNotProvided");     // Isn't used
            builder.Delete("Account.ChangePassword.Errors.EmailNotFound");          // Isn't used
            builder.Delete("Account.ChangePassword.Errors.OldPasswordDoesntMatch"); // Isn't used

            builder.AddOrUpdate("Identity.Error.ConcurrencyFailure",
                "A concurrency failure has occured while trying to save your data.",
                "Beim Speichern Ihrer Daten ist ein Fehler durch gleichzeitigen Zugriff aufgetreten.");
            builder.AddOrUpdate("Identity.Error.DefaultError",
                "An error has occurred. Please retry the operation.",
                "Es ist ein Fehler aufgetreten. Bitte führen Sie den Vorgang erneut durch.");
            builder.AddOrUpdate("Identity.Error.DuplicateRoleName",
                "The rolename '{0}' already exists.",
                "Der Name '{0}' wird bereits für eine andere Kundengruppe verwendet.");
            builder.AddOrUpdate("Identity.Error.DuplicateUserName",
                "The username '{0}' already exists.",
                "Der Benutzername '{0}' wird bereits verwendet.");
            builder.AddOrUpdate("Identity.Error.DuplicateEmail",
                "The email '{0}' already exists",
                "Die E-Mail-Adresse '{0}' wird bereits verwendet.");
            builder.AddOrUpdate("Identity.Error.InvalidEmail",
                "Email is not valid.",
                "Keine gültige E-Mail-Adresse.");
            builder.AddOrUpdate("Identity.Error.InvalidRoleName",
                "The name '{0}' is not valid for customer roles.",
                "Der Name '{0}' ist für Kundengruppen nicht gültig.");
            builder.AddOrUpdate("Identity.Error.InvalidToken",
                "Token is not valid.",
                "Das Token ist nicht gültig.");
            builder.AddOrUpdate("Identity.Error.InvalidUserName",
                "The username '{0}' is not valid.",
                "Der Benutzername '{0}' ist nicht gültig.");
            builder.AddOrUpdate("Identity.Error.LoginAlreadyAssociated",
                "The customer is already registered.",
                "Der Kunde ist bereits registriert.");
            builder.AddOrUpdate("Identity.Error.PasswordMismatch",
                "The password and confirmation do not match.",
                "Passwort und Bestätigung stimmen nicht überein.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresDigit",
                "The password must contain a digit.",
                "Das Passwort muss mind. eine Ziffer enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresLower",
                "The password must contain a lowercase letter.",
                "Das Passwort muss mind. einen Kleinbuchstaben enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresNonAlphanumeric",
                "The password must contain a non alphanumeric character.",
                "Das Passwort muss mind. ein Sonderzeichen enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresUniqueChars",
                "The password must contain at least {0} unique characters.",
                "Das Passwort muss mindestens {0} eindeutige Zeichen enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresUpper",
                "The password must contain a capital letter.",
                "Das Passwort muss mind. einen Großbuchstaben enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordTooShort",
                "The password is too short. It must contain at least {0} characters.",
                "Das Passwort ist zu kurz. Es muss mindestens {0} Zeichen enthalten.");
            builder.AddOrUpdate("Identity.Error.RecoveryCodeRedemptionFailed",
                "The redemption of the recovery code failed.",
                "Die Eingabe des Wiederherstellungscodes ist fehlgeschlagen.");
            builder.AddOrUpdate("Identity.Error.UserAlreadyHasPassword",
                "The user already has a password.",
                "Der Benutzer verfügt bereits über ein Passwort.");
            builder.AddOrUpdate("Identity.Error.UserAlreadyInRole",
                "The user has already been assigned to the customer role '{0}'.",
                "Der Benutzer wurde der Kundengruppe '{0}' bereits zugewiesen.");
            builder.AddOrUpdate("Identity.Error.UserLockoutNotEnabled",
                "User lockout is not enabled.",
                "Die Benutzersperrung ist nicht aktiviert.");
            builder.AddOrUpdate("Identity.Error.UserNotInRole",
                "You do not have the necessary permissions to perform this operation.",
                "Sie verfügen nicht über die erforderlichen Rechte, diesen Vorgang durchzuführen.");

            builder.AddOrUpdate("Account.Register.Result.Disabled",
                "Registration is not allowed at the moment.",
                "Die Registrierung ist momentan nicht erlaubt.");

            builder.AddOrUpdate("ActivityLog.PublicStore.LoginExternal", "Logged in with {0}", "Eingeloggt mit {0}");
            builder.AddOrUpdate("Account.Login.CheckEmailAccount",
                "The credentials provided are incorrect or you have not activated your account yet. Please check your email inbox and confirm the registration.",
                "Die eingegebenen Benutzerdaten sind nicht korrekt oder Sie haben Ihr Konto noch nicht aktiviert. Bitte prüfen Sie Ihren Email-Posteingang und bestätigen Sie die Registrierung.");

            builder.AddOrUpdate("Admin.System.QueuedEmails.Fields.Attachments",
                "File attachments",
                "Dateianhänge");

            builder.Delete("Admin.Configuration.Settings.CustomerUser.MinDigitsInPassword");
            builder.Delete("Admin.Configuration.Settings.CustomerUser.MinSpecialCharsInPassword");
            builder.Delete("Admin.Configuration.Settings.CustomerUser.MinUppercaseCharsInPassword");
            builder.Delete("Admin.Configuration.Settings.CustomerUser.MinDigitsInPassword.Hint");
            builder.Delete("Admin.Configuration.Settings.CustomerUser.MinSpecialCharsInPassword.Hint");
            builder.Delete("Admin.Configuration.Settings.CustomerUser.MinUppercaseCharsInPassword.Hint");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.PasswordRequireDigit",
                "Password requires digit",
                "Passwort erfordert Ziffer",
                "Specifies that passwords must contain at least one digit.",
                "Legt fest, dass Passwörter mindestens eine Ziffer enthalten müssen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.PasswordRequireUppercase",
                "Password requires uppercase",
                "Passwort erfordert Großbuchstaben",
                "Specifies that passwords must contain at least one uppercase letter.",
                "Legt fest, dass Passwörter mindestens einen Großbuchstaben enthalten müssen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.PasswordRequireLowercase",
                "Password requires lowercase",
                "Passwort erfordert Kleinbuchstaben",
                "Specifies that passwords must contain at least one lowercase letter.",
                "Legt fest, dass Passwörter mindestens einen Kleinbuchstaben enthalten müssen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.PasswordRequiredUniqueChars",
                "Password requires unique chars",
                "Passwort erfordert eindeutige Zeichen",
                "Specifies the minimum number of unique characters which a password must contain.",
                "Legt die Mindestanzahl der eindeutigen Zeichen fest, die ein Passwort enthalten muss.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.PasswordRequireNonAlphanumeric",
                "Password requires special characters",
                "Passwort erfordert Sonderzeichen",
                "Specifies that passwords must contain at least one non alphanumeric character.",
                "Legt fest, dass Passwörter mindestens ein nicht alphanumerisches Zeichen enthalten müssen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.News", "News", "News");

            builder.AddOrUpdate("Account.Fields.Password", "Password security", "Passwortsicherheit");
            builder.AddOrUpdate("Account.ChangePassword", "Change password", "Passwort ändern");

            #endregion

            #region DataGrid

            builder.AddOrUpdate("Admin.DataGrid.ResetState", "Reset", "Zurücksetzen");
            builder.AddOrUpdate("Admin.DataGrid.NoData", "No data", "Keine Daten");
            builder.AddOrUpdate("Admin.DataGrid.VBorders", "Row lines", "Zeilen liniert");
            builder.AddOrUpdate("Admin.DataGrid.HBorders", "Column lines", "Spalten liniert");
            builder.AddOrUpdate("Admin.DataGrid.Striped", "Striped", "Gestreift");
            builder.AddOrUpdate("Admin.DataGrid.Hover", "Hover", "Hover");
            builder.AddOrUpdate("Admin.DataGrid.PagerPos", "Pager", "Pager");
            builder.AddOrUpdate("Admin.DataGrid.PagerTop", "Top", "Oben");
            builder.AddOrUpdate("Admin.DataGrid.PagerBottom", "Bottom", "Unten");
            builder.AddOrUpdate("Admin.DataGrid.PagerBoth", "Top & bottom", "Oben & unten");
            builder.AddOrUpdate("Admin.DataGrid.XPerPage", "<span class='fwm'>{0}</span> per page", "<span class='fwm'>{0}</span> pro Seite");
            builder.AddOrUpdate("Admin.DataGrid.DisplayingItems", "Displaying items {0}-{1} of {2}", "Zeige Datensätze {0}-{1} von {2}");
            builder.AddOrUpdate("Admin.DataGrid.DisplayingItemsShort", "{0}-{1} of {2}", "{0}-{1} von {2}");
            builder.AddOrUpdate("Admin.DataGrid.ConfirmDelete", "Do you really want to delete the item permanently?", "Soll der Datensatz wirklich unwiderruflich gelöscht werden?");
            builder.AddOrUpdate("Admin.DataGrid.ConfirmDeleteMany", "Do you really want to delete the selected {0} items permanently?", "Sollen die gewählten {0} Datensätze wirklich unwiderruflich gelöscht werden?");
            builder.AddOrUpdate("Admin.DataGrid.DeleteSuccess", "{0} items successfully deleted.", "{0} Datensätze erfolgreich gelöscht.");

            #endregion

            #region Fixes

            builder.AddOrUpdate("Enums.LogLevel.Debug", "Debug", "Debug");
            builder.AddOrUpdate("Enums.LogLevel.Information", "Info", "Info");

            builder.AddOrUpdate("ActivityLog.ImportThemeVars", "Imported {0} variables for theme '{1}'.", "{0} Variablen für Theme '{1}' importiert.");
            builder.AddOrUpdate("ActivityLog.ExportThemeVars", "Successfully exported theme '{0}'.", "Theme '{0}' erfolgreich exportiert.");

            builder.Delete("Admin.Catalog.Products.Fields.QuantiyControlType");
            builder.Delete("Admin.Catalog.Products.Fields.QuantiyControlType.Hint");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.QuantityControlType",
                "Control type",
                "Steuerelement",
                "Specifies the control type to enter the quantity.",
                "Bestimmt das Steuerelement für die Angabe der Bestellmenge.");

            builder.AddOrUpdate("Validation.MustBeANumber", "'{PropertyName}' muss be a number.", "'{PropertyName}' muss eine Zahl sein.");
            builder.AddOrUpdate("Validation.NonPropertyMustBeANumber", "The field muss be a number.", "Das Feld muss eine Zahl sein.");

            builder.AddOrUpdate("Admin.System.Maintenance.SqlQuery.Succeeded",
                "The SQL command was executed successfully. Rows affected: {0}.",
                "Die SQL-Anweisung wurde erfolgreich ausgeführt. Betroffene Zeilen: {0}.");

            builder.AddOrUpdate("Admin.Orders.OrderNotes.Fields.Note",
                "Note",
                "Notiz",
                "The message or note to this order.",
                "Die Nachricht oder Notiz zu diesem Auftrag.");

            builder.AddOrUpdate("Admin.Orders.OrderNotes.Fields.DisplayToCustomer",
                "Display to customer",
                "Für den Benutzer sichtbar",
                "A value indicating whether to display this order note to a customer.",
                "Legt fest, ob die Notiz für den Benutzer sichtbar ist.");

            builder.AddOrUpdate("Admin.System.Log.Fields.Customer",
                "Customer",
                "Kunde",
                "Customer who caused the exception.",
                "Kunde, der die Ausnahme verursacht hat.");

            builder.AddOrUpdate("Admin.System.Log.Fields.UserName",
                "Username",
                "Benutzername");

            builder.AddOrUpdate("Admin.Orders.List.GoDirectlyToNumber",
                "Search by order number",
                "Nach Auftragsnummer suchen",
                "Opens directly the details of the order with the order number or order reference number.",
                "Öffnet direkt die Details zum Auftrag mit der Auftrags- oder Bestellreferenznummer.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Media.FileStorage",
                "Storage",
                "Dateispeicher");

            builder.AddOrUpdate("Admin.Configuration.Settings.Media.CurrentStorageLocation",
                "The current storage provider is",
                "Der aktuelle Speicheranbieter ist");

            builder.AddOrUpdate("Admin.Configuration.Settings.Media.StorageProvider",
                "Change storage",
                "Speicher wechseln",
                "Specifies the new storage provider for media file like images.",
                "Legt den neuen Speicheranbieter für Mediendateien wie z.B. Bilder fest.");

            builder.AddOrUpdate("Admin.ContentManagement.Blog.BlogPosts.Fields.PreviewDisplayType",
                "Preview display type",
                "Vorschau-Darstellung",
                "Specifies display type of the preview for a blog item.",
                "Legt die Darstellung der Vorschau für einen Blog-Eintrag fest.");

            builder.AddOrUpdate("Admin.ContentManagement.Blog.BlogPosts.Fields.DisplayTagsInPreview",
                "Display tags on preview",
                "Tags in Vorschau anzeigen",
                "Specifies whether tags are display in the preview for blog item.",
                "Legt fest, ob Tags in der Vorschau eines Blog-Eintrags angezeigt werden.");

            builder.AddOrUpdate("Reviews.Helpfulness.SuccessfullyVoted",
                "Thank you for your voting.",
                "Danke für Ihre Beurteilung.");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.Fields.Sku", "SKU", "SKU");

            #endregion

            #region Obsolete

            builder.Delete(
                "Admin.Catalog.Categories.BackToList",
                "Admin.Catalog.Manufacturers.BackToList",
                "Admin.Catalog.Categories.Info",
                "Admin.Catalog.Manufacturers.Info",
                "Admin.Catalog.Categories.Products.SaveBeforeEdit",
                "Admin.Catalog.Manufacturers.Products.SaveBeforeEdit",
                "Admin.Catalog.Attributes.SpecificationAttributes.BackToList",
                "Admin.Catalog.Attributes.SpecificationAttributes.Info",
                "Admin.Promotions.Discounts.BackToList",
                "Admin.Promotions.Discounts.Info",
                "Admin.Catalog.Attributes.ProductAttributes.BackToList",
                "Admin.ContentManagement.Forums.ForumGroup.BackToList",
                "Admin.ContentManagement.Forums.Forum.BackToList",
                "Admin.GiftCards.Info",
                "Admin.GiftCards.BackToList",
                "Admin.Common.FilesDeleted",
                "Admin.Common.FoldersDeleted",
                "Admin.SalesReport.NeverSold.Fields.Name",
                "Admin.Orders.Info",
                "Admin.Orders.OrderNotes.Fields.AddOrderNoteDisplayToCustomer",
                "Admin.Orders.OrderNotes.Fields.AddOrderNoteMessage",
                "Admin.Orders.Products.AddNew.Name",
                "Admin.Orders.Products.AddNew.SKU",
                "Admin.Orders.Products.AddNew.Title1",
                "Admin.Orders.Products.AddNew.Note1",
                "Admin.Orders.Products.AddNew.BackToList",
                "Admin.ReturnRequests.BackToList",
                "Admin.RecurringPayments.BackToList",
                "Admin.Configuration.Languages.BackToList",
                "Admin.Catalog.ProductReviews.BackToList",
                "Admin.System.QueuedEmails.BackToList",
                "Admin.Orders.List.OrderGuid",
                "Admin.Orders.List.OrderGuid.Hint",
                "Admin.DataExchange.Export.FolderName.Validate",
                "Admin.ContentManagement.Topics.BackToList",
                "Admin.Configuration.Currencies.BackToList",
                "Admin.Configuration.Currencies.Fields.PrimaryStoreCurrencyStores",
                "Admin.Configuration.Currencies.Fields.PrimaryStoreCurrencyStores.Hint",
                "Admin.Configuration.Currencies.Fields.PrimaryExchangeRateCurrencyStores",
                "Admin.Configuration.Currencies.Fields.PrimaryExchangeRateCurrencyStores.Hint",
                "Admin.Configuration.Currencies.DeleteOrPublishStoreConflict",
                "Admin.Configuration.DeliveryTime.BackToList",
                "Admin.Configuration.DeliveryTimes.Fields.DisplayLocale",
                "Admin.Configuration.Stores.BackToList",
                "Admin.Configuration.EmailAccounts.BackToList",
                "Admin.Customers.Customers.ActivityLog.ActivityLogType",
                "Admin.Customers.Customers.ActivityLog.Comment",
                "Admin.ContentManagement.News.Blog.BlogPosts.Fields.PreviewDisplayType",
                "Admin.ContentManagement.News.Blog.BlogPosts.Fields.DisplayTagsInPreview",
                "Admin.ContentManagement.News.Blog.BlogPosts.Fields.IsPublished",
                "Admin.ContentManagement.Blog.BlogPosts.BackToList",
                "Admin.ContentManagement.News.NewsItems.BackToList"
            );

            #endregion
        }

        private static async Task RemoveTelerikResources(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.LocaleStringResources
                .Where(x => x.ResourceName.StartsWith("Admin.Telerik."))
                .ExecuteDeleteAsync(cancelToken);

            await context.LocaleStringResources
                .Where(x => x.ResourceName.StartsWith("Telerik."))
                .ExecuteDeleteAsync(cancelToken);

            await context.SaveChangesAsync(cancelToken);
        }

        private static async Task MigrateEnumResources(SmartDbContext context, CancellationToken cancelToken = default)
        {
            var table = context.LocaleStringResources;
            var markerEntity = await table.FirstOrDefaultAsync(x => x.ResourceName == "Enums.Smartstore.__Migrated__", cancelToken);
            if (markerEntity != null)
            {
                // (perf) Don't migrate again.
                return;
            }

            var resources = await table
                .Where(x => x.ResourceName.StartsWith("Enums.SmartStore."))
                .ToListAsync(cancelToken);

            if (resources.Count > 0)
            {
                var map = GetEnumNameMap();
                var toAdd = new List<LocaleStringResource>();

                foreach (var entity in resources)
                {
                    var key = entity.ResourceName;
                    var lastDotIndex = key.LastIndexOf('.');
                    var lastPart = key[(lastDotIndex + 1)..];

                    // Trim "Enums." and last Part
                    key = key[6..lastDotIndex];

                    if (map.TryGetValue(key, out var newName))
                    {
                        // We don't update, but add new entries to keep Smartstore classic projects intact.
                        toAdd.Add(new LocaleStringResource
                        {
                            ResourceName = $"Enums.{newName}.{lastPart}",
                            IsFromPlugin = entity.IsFromPlugin,
                            IsTouched = entity.IsTouched,
                            LanguageId = entity.LanguageId,
                            ResourceValue = entity.ResourceValue
                        });
                    }
                }

                toAdd.Add(new LocaleStringResource
                {
                    ResourceName = "Enums.Smartstore.__Migrated__",
                    LanguageId = toAdd.FirstOrDefault()?.LanguageId ?? 1,
                    ResourceValue = string.Empty
                });
                table.AddRange(toAdd);

                await context.SaveChangesAsync(cancelToken);
            }
        }

        private static Dictionary<string, string> GetEnumNameMap()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SmartStore.Core.Domain.Catalog.AttributeControlType"] = nameof(AttributeControlType),
                ["SmartStore.Core.Domain.Catalog.BackorderMode"] = nameof(BackorderMode),
                ["SmartStore.Core.Domain.Catalog.DownloadActivationType"] = nameof(DownloadActivationType),
                ["SmartStore.Core.Domain.Catalog.GiftCardType"] = nameof(GiftCardType),
                ["SmartStore.Core.Domain.Catalog.LowStockActivity"] = nameof(LowStockActivity),
                ["SmartStore.Core.Domain.Catalog.ManageInventoryMethod"] = nameof(ManageInventoryMethod),
                ["SmartStore.Core.Domain.Catalog.ProductSortingEnum"] = nameof(ProductSortingEnum),
                ["SmartStore.Core.Domain.Catalog.ProductType"] = nameof(ProductType),
                ["SmartStore.Core.Domain.Catalog.ProductVariantAttributeValueType"] = nameof(ProductVariantAttributeValueType),
                ["SmartStore.Core.Domain.Catalog.RecurringProductCyclePeriod"] = nameof(RecurringProductCyclePeriod),
                ["SmartStore.Core.Domain.Common.PageTitleSeoAdjustment"] = nameof(PageTitleSeoAdjustment),
                ["SmartStore.Core.Domain.Customers.CustomerNameFormat"] = nameof(CustomerNameFormat),
                ["SmartStore.Core.Domain.Customers.PasswordFormat"] = nameof(PasswordFormat),
                ["SmartStore.Core.Domain.Discounts.DiscountLimitationType"] = nameof(DiscountLimitationType),
                ["SmartStore.Core.Domain.Discounts.DiscountType"] = nameof(DiscountType),
                ["SmartStore.Core.Domain.Localization.DefaultLanguageRedirectBehaviour"] = nameof(DefaultLanguageRedirectBehaviour),
                ["SmartStore.Core.Domain.Logging.LogLevel"] = nameof(LogLevel),
                ["SmartStore.Core.Domain.Orders.OrderStatus"] = nameof(OrderStatus),
                ["SmartStore.Core.Domain.Orders.ReturnRequestStatus"] = nameof(ReturnRequestStatus),
                ["SmartStore.Core.Domain.Payments.PaymentStatus"] = nameof(PaymentStatus),
                ["SmartStore.Core.Domain.Security.UserRegistrationType"] = nameof(UserRegistrationType),
                ["SmartStore.Core.Domain.Shipping.ShippingStatus"] = nameof(ShippingStatus),
                ["SmartStore.Core.Domain.Tax.TaxBasedOn"] = nameof(TaxBasedOn),
                ["SmartStore.Core.Domain.Tax.TaxDisplayType"] = nameof(TaxDisplayType),
                ["SmartStore.Core.Domain.Tax.VatNumberStatus"] = nameof(VatNumberStatus),
                ["SmartStore.Core.Domain.Seo.CanonicalHostNameRule"] = nameof(CanonicalHostNameRule),
                ["SmartStore.Core.Domain.Catalog.SubCategoryDisplayType"] = nameof(SubCategoryDisplayType),
                ["SmartStore.Core.Domain.Catalog.PriceDisplayType"] = nameof(PriceDisplayType),
                ["SmartStore.Core.Domain.DataExchange.ExportEntityType"] = nameof(ExportEntityType),
                ["SmartStore.Core.Domain.DataExchange.ExportDeploymentType"] = nameof(ExportDeploymentType),
                ["SmartStore.Core.Domain.DataExchange.ExportDescriptionMerging"] = nameof(ExportDescriptionMerging),
                ["SmartStore.Core.Domain.DataExchange.ExportAttributeValueMerging"] = nameof(ExportAttributeValueMerging),
                ["SmartStore.Core.Domain.DataExchange.ExportHttpTransmissionType"] = nameof(ExportHttpTransmissionType),
                ["SmartStore.Core.Domain.DataExchange.ExportOrderStatusChange"] = nameof(ExportOrderStatusChange),
                ["SmartStore.Core.Domain.DataExchange.RelatedEntityType"] = nameof(RelatedEntityType),
                ["SmartStore.Core.Domain.DataExchange.ImportFileType"] = nameof(ImportFileType),
                ["SmartStore.Core.Domain.DataExchange.ImportEntityType"] = nameof(ImportEntityType),
                ["SmartStore.Core.Domain.Orders.CheckoutNewsletterSubscription"] = nameof(CheckoutNewsletterSubscription),
                ["SmartStore.Core.Domain.Orders.CheckoutThirdPartyEmailHandOver"] = nameof(CheckoutThirdPartyEmailHandOver),
                ["SmartStore.Core.Domain.Customers.CustomerNumberMethod"] = nameof(CustomerNumberMethod),
                ["SmartStore.Core.Domain.Customers.CustomerNumberVisibility"] = nameof(CustomerNumberVisibility),
                ["SmartStore.Core.Domain.Tax.AuxiliaryServicesTaxType"] = nameof(AuxiliaryServicesTaxType),
                ["SmartStore.Core.Domain.Catalog.GridColumnSpan"] = nameof(GridColumnSpan),
                ["SmartStore.Core.Domain.Directory.CurrencyRoundingRule"] = nameof(CurrencyRoundingRule),
                ["SmartStore.Core.Domain.Orders.ShoppingCartType"] = nameof(ShoppingCartType),
                ["SmartStore.Core.Domain.Payments.CapturePaymentReason"] = nameof(CapturePaymentReason),
                ["SmartStore.Core.Domain.Customers.WalletPostingReason"] = nameof(WalletPostingReason),
                ["SmartStore.Core.Domain.Customers.CustomerLoginType"] = nameof(CustomerLoginType),
                ["SmartStore.Core.Domain.Catalog.ProductVisibility"] = nameof(ProductVisibility),
                ["SmartStore.Core.Domain.Catalog.ProductCondition"] = nameof(ProductCondition),
                ["SmartStore.Core.Domain.Directory.DeliveryTimesPresentation"] = nameof(DeliveryTimesPresentation),
                ["SmartStore.Core.Domain.Catalog.AttributeChoiceBehaviour"] = nameof(AttributeChoiceBehaviour),
                ["SmartStore.Rules.RuleScope"] = nameof(RuleScope),
                ["SmartStore.Services.Payments.RecurringPaymentType"] = nameof(RecurringPaymentType),

                ["SmartStore.Core.Search.Facets.FacetSorting"] = nameof(FacetSorting),
                ["SmartStore.Core.Search.Facets.FacetTemplateHint"] = nameof(FacetTemplateHint),
                ["SmartStore.Core.Search.IndexingStatus"] = nameof(IndexingStatus),
                ["SmartStore.Core.Search.SearchMode"] = nameof(SearchMode),

                //["SmartStore.NewsImporter.Core.ImageEmbeddingType"] = nameof(ImageEmbeddingType), // NewsImporter, currently unclear whether the module will be ported.

                ["SmartStore.ContentSlider.Domain.ProductDisplayType"] = "ProductDisplayType",

                ["SmartStore.Plugin.Shipping.Fedex.DropoffType"] = "DropoffType",
                ["SmartStore.Plugin.Shipping.Fedex.PackingType"] = "PackingType",

                ["SmartStore.Core.Domain.Blogs.PreviewDisplayType"] = "PreviewDisplayType",

                ["SmartStore.Core.Domain.Forums.ForumTopicType"] = "ForumTopicType",
                ["SmartStore.Core.Domain.Forums.ForumDateFilter"] = "ForumDateFilter",
                ["SmartStore.Core.Domain.Forums.EditorType"] = "EditorType",
                ["SmartStore.Core.Domain.Forums.ForumTopicSorting"] = "ForumTopicSorting",

                ["SmartStore.PageBuilder.Blocks.CategoryDisplayType"] = "CategoryDisplayType",
                ["SmartStore.PageBuilder.Blocks.CategoryPickingType"] = "CategoryPickingType",
                ["SmartStore.PageBuilder.Blocks.IconAlignment"] = "IconAlignment",
                ["SmartStore.PageBuilder.Blocks.IconDisplayType"] = "IconDisplayType",
                ["SmartStore.PageBuilder.Blocks.ProductListDisplayType"] = "ProductListDisplayType",
                ["SmartStore.PageBuilder.Blocks.ProductPickingType"] = "ProductPickingType",
                ["SmartStore.PageBuilder.Models.MegaSizeTypes"] = "MegaSizeTypes",
                ["SmartStore.PageBuilder.StoryTemplateGroup"] = "StoryTemplateGroup",
                ["SmartStore.PageBuilder.Blocks.BrandListDisplayType"] = "BrandListDisplayType",
                ["SmartStore.PageBuilder.Blocks.ButtonAlignment"] = "ButtonAlignment",
                ["SmartStore.PageBuilder.Blocks.ButtonIconAlignment"] = "ButtonIconAlignment",
                ["SmartStore.PageBuilder.Blocks.TitleDisplayType"] = "TitleDisplayType",
                ["SmartStore.PageBuilder.Models.BoxImagePlacement"] = "BoxImagePlacement",
                ["SmartStore.PageBuilder.Models.GradientRepeat"] = "GradientRepeat",
                ["SmartStore.PageBuilder.Blocks.GalleryStyle"] = "GalleryStyle",

                ["SmartStore.MegaMenu.Domain.AlignX"] = "AlignX",
                ["SmartStore.MegaMenu.Domain.AlignY"] = "AlignY",
                ["SmartStore.MegaMenu.Domain.TeaserType"] = "TeaserType",
                ["SmartStore.MegaMenu.Domain.TeaserRotatorItemSelectType"] = "TeaserRotatorItemSelectType",
                ["SmartStore.MegaMenu.Settings.BrandDisplayType"] = "BrandDisplayType",
                ["SmartStore.MegaMenu.Settings.BrandPlacement"] = "BrandPlacement",
                ["SmartStore.MegaMenu.Settings.BrandRows"] = "BrandRows",
                ["SmartStore.MegaMenu.Settings.BrandSortOrder"] = "BrandSortOrder",

                ["SmartStore.MegaSearch.TextAnalysisType"] = "TextAnalysisType",
            };
        }
    }
}
