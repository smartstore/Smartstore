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
using Smartstore.Core.Configuration;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Seo;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await MigrateEnumResources(context, cancelToken);
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            // Remark: In classic code, we always made the mistake to query for FirstOrDefault and thus ignoring multistore settings.
            var settings = context.Set<Setting>();

            var minDigitsInPasswordSettings = await settings.Where(x => x.Name == "CustomerSettings.MinDigitsInPassword").ToListAsync(cancellationToken: cancelToken);
            if (minDigitsInPasswordSettings.Any())
            {
                settings.RemoveRange(minDigitsInPasswordSettings);
            }

            var minSpecialCharsInPassword = await settings.Where(x => x.Name == "CustomerSettings.MinSpecialCharsInPassword").ToListAsync(cancellationToken: cancelToken);
            if (minSpecialCharsInPassword.Any())
            {
                settings.RemoveRange(minSpecialCharsInPassword);
            }

            var minUppercaseCharsInPassword = await settings.Where(x => x.Name == "CustomerSettings.MinUppercaseCharsInPassword").ToListAsync(cancellationToken: cancelToken);
            if (minUppercaseCharsInPassword.Any())
            {
                settings.RemoveRange(minUppercaseCharsInPassword);
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

            var store = await context.Stores
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .FirstOrDefaultAsync(cancelToken);

            if (store != null)
            {
                var settingValues = new Dictionary<string, string>
                {
                    { "CurrencySettings.PrimaryCurrencyId", store.PrimaryStoreCurrencyId.ToString() },
                    { "CurrencySettings.PrimaryExchangeCurrencyId", store.PrimaryExchangeRateCurrencyId.ToString() }
                };

                foreach (var pair in settingValues)
                {
                    var setting = await settings.FirstOrDefaultAsync(x => x.Name == pair.Key && x.StoreId == 0, cancelToken);
                    if (setting != null)
                    {
                        setting.Value = pair.Value;
                    }
                    else
                    {
                        settings.Add(new Setting { Name = pair.Key, Value = pair.Value });
                    }
                }
            }

            await context.SaveChangesAsync(cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            // TODO: (core) Uncomment all resource deletions before first public release.

            #region General

            // TODO: (core) Delete all Telerik language resources (???)

            builder.AddOrUpdate("Admin.Catalog.Products.Orders.NoOrdersAvailable",
                "There are no orders for this product yet.", 
                "Für dieses Produkt existieren noch keine Bestellungen.");
            
            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryStoreCurrencyId");
            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryStoreCurrencyId.Hint");
            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryExchangeRateCurrencyId");
            builder.Delete("Admin.Configuration.Stores.Fields.PrimaryExchangeRateCurrencyId.Hint");

            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.CycleInfo", "Interval", "Interval");
            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.CyclesRemaining", "Remaining", "Verbleibend");

            builder.AddOrUpdate("Admin.Tax.Categories.NoDuplicatesAllowed",
                "A tax category with this name already exists. Please choose another name.",
                "Eine Steuerklasse mit diesem Namen existiert bereits. Bitte wählen Sie einen anderen Namen.");

            builder.AddOrUpdate("Admin.Customers.Customers.List.SearchCustomerNumber", "Customer number", "Kundennummer");

            builder.AddOrUpdate("Admin.Configuration.EmailAccounts.CantDeleteDefault",
                "The default email account cannot be deleted.",
                "Das Standard-Email-Konto kann nicht gelöscht werden.");

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

            builder.AddOrUpdate("Admin.Configuration.Currencies.ApplyRate.Error", "An error occurred when applying the rate.", "Bei der Aktualisierung der Rate ist ein Fehler aufgetreten.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.CannotDeleteAssociated",
                "Currencies that have already been assigned to a store cannot be deleted. Remove the assignment first.",
                "Währungen, die bereits einem Store zugewiesen wurde,n können nicht gelöscht werden. Entfernen Sie zunächst die Zuordnung.");

            builder.AddOrUpdate("Admin.Configuration.Measures.QuantityUnits.CantDeleteDefault",
                "The default quantity unit can't be deleted. Specify another standard quantity unit beforehand.",
                "Die Standard-Verpackungseinheit kann nicht gelöscht werden. Bestimmen Sie zuvor eine andere Standard-Verpackungseinheit.");

            builder.AddOrUpdate("Admin.Configuration.DeliveryTimes.CantDeleteDefault",
                "The default delivery time can't be deleted. Specify another standard delivery time beforehand.",
                "Die Standard-Lieferzeit kann nicht gelöscht werden. Bestimmen Sie zuvor eine andere Standard-Lieferzeit.");
           
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
            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.IsCurrentVersion", "Current version", "Aktuelle Version");
            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.Create", "Create backup", "Sicherung erstellen");

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
            builder.Delete("Account.ChangePassword.Errors.PasswordIsNotProvided");  // Isn't used

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
            

            // INFO: New resources.
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

            builder.AddOrUpdate("Account.Fields.Password", "Password management", "Passwortverwaltung");

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

            #endregion

            #region Obsolete

            // Deletion at this time is a bit confusing if you run Classic and Core with the same database and the strings in Classic are suddenly gone.
            //builder.Delete(
            //    "Admin.Catalog.Categories.BackToList",
            //    "Admin.Catalog.Manufacturers.BackToList",
            //    "Admin.Catalog.Categories.Info",
            //    "Admin.Catalog.Manufacturers.Info",
            //    "Admin.Catalog.Categories.Products.SaveBeforeEdit",
            //    "Admin.Catalog.Manufacturers.Products.SaveBeforeEdit",
            //    "Admin.Catalog.Attributes.SpecificationAttributes.BackToList",
            //    "Admin.Catalog.Attributes.SpecificationAttributes.Info",
            //    "Admin.Promotions.Discounts.BackToList",
            //    "Admin.Promotions.Discounts.Info",
            //    "Admin.Catalog.Attributes.ProductAttributes.BackToList",
            //    "Admin.ContentManagement.Forums.ForumGroup.BackToList",
            //    "Admin.ContentManagement.Forums.Forum.BackToList",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicType.Announcement",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicType.Normal",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicType.Sticky",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.LastMonth",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.LastSixMonths",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.LastThreeMonths",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.LastTwoWeeks",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.LastVisit",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.LastWeek",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.LastYear",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumDateFilter.Yesterday",
            //    "Enums.SmartStore.Core.Domain.Forums.EditorType.BBCodeEditor",
            //    "Enums.SmartStore.Core.Domain.Forums.EditorType.HtmlEditor",
            //    "Enums.SmartStore.Core.Domain.Forums.EditorType.SimpleTextBox",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.CreatedOnAsc",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.CreatedOnDesc",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.Initial",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.PostsAsc",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.PostsDesc",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.Relevance",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.SubjectAsc",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.SubjectDesc",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.UserNameAsc",
            //    "Enums.SmartStore.Core.Domain.Forums.ForumTopicSorting.UserNameDesc",
            //    "Admin.GiftCards.Info",
            //    "Admin.GiftCards.BackToList",
            //    "Admin.Common.FilesDeleted",
            //    "Admin.Common.FoldersDeleted",
            //    "Admin.SalesReport.NeverSold.Fields.Name",
            //    "Admin.Orders.Info",
            //    "Admin.Orders.OrderNotes.Fields.AddOrderNoteDisplayToCustomer",
            //    "Admin.Orders.OrderNotes.Fields.AddOrderNoteMessage",
            //    "Admin.Catalog.Products.List.SearchProductName",
            //    "Admin.Catalog.Products.List.SearchCategory",
            //    "Admin.Catalog.Products.List.SearchManufacturer",
            //    "Admin.Catalog.Products.List.SearchProductType",
            //    "Admin.Orders.Products.AddNew.Name",
            //    "Admin.Orders.Products.AddNew.SKU",
            //    "Admin.Orders.Products.AddNew.Title1",
            //    "Admin.Orders.Products.AddNew.Note1",
            //    "Admin.Orders.Products.AddNew.BackToList",
            //    "Admin.ReturnRequests.BackToList",
            //    "Admin.RecurringPayments.BackToList",
            //    "Admin.Configuration.Languages.BackToList",
            //    "Admin.Catalog.ProductReviews.BackToList",
            //    "Admin.System.QueuedEmails.BackToList",
            //    "Admin.Orders.List.OrderGuid",
            //    "Admin.Orders.List.OrderGuid.Hint",
            //    "Admin.DataExchange.Export.FolderName.Validate",
            //    "Admin.ContentManagement.Topics.BackToList",
            //    "Admin.Configuration.Currencies.BackToList",
            //    );


            #endregion
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
            // TODO: (mh) (core) Add missing enum localization map entries when they are available.
            // TODO: (mh) (core) Assign an alias names to enums in plugins if enum name is too generic.
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
                //["SmartStore.Plugin.Shipping.Fedex.DropoffType"] = nameof(DropoffType),
                //["SmartStore.Plugin.Shipping.Fedex.PackingType"] = nameof(PackingType),
                ["SmartStore.Services.Payments.RecurringPaymentType"] = nameof(RecurringPaymentType),
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
                //["SmartStore.MegaSearch.Services.IndexingStatus"] = nameof(IndexingStatus),
                //["SmartStore.NewsImporter.Core.ImageEmbeddingType"] = nameof(ImageEmbeddingType),
                ["SmartStore.Core.Search.SearchMode"] = nameof(SearchMode),
                ["SmartStore.Core.Domain.Catalog.GridColumnSpan"] = nameof(GridColumnSpan),
                //["SmartStore.MegaSearch.TextAnalysisType"] = nameof(TextAnalysisType),
                ["SmartStore.Core.Search.Facets.FacetSorting"] = nameof(FacetSorting),
                ["SmartStore.Core.Search.Facets.FacetTemplateHint"] = nameof(FacetTemplateHint),
                //["SmartStore.MegaMenu.Domain.AlignX"] = nameof(AlignX),
                //["SmartStore.MegaMenu.Domain.AlignY"] = nameof(AlignY),
                //["SmartStore.MegaMenu.Domain.TeaserRotatorItemSelectType"] = nameof(TeaserRotatorItemSelectType),
                //["SmartStore.MegaMenu.Domain.TeaserType"] = nameof(TeaserType),
                //["SmartStore.ContentSlider.Domain.ProductDisplayType"] = nameof(ProductDisplayType),
                ["SmartStore.Core.Domain.Directory.CurrencyRoundingRule"] = nameof(CurrencyRoundingRule),
                //["SmartStore.MegaMenu.Settings.BrandDisplayType"] = nameof(BrandDisplayType),
                //["SmartStore.MegaMenu.Settings.BrandPlacement"] = nameof(BrandPlacement),
                //["SmartStore.MegaMenu.Settings.BrandRows"] = nameof(BrandRows),
                //["SmartStore.MegaMenu.Settings.BrandSortOrder"] = nameof(BrandSortOrder),
                ["SmartStore.Core.Domain.Orders.ShoppingCartType"] = nameof(ShoppingCartType),
                ["SmartStore.Core.Domain.Payments.CapturePaymentReason"] = nameof(CapturePaymentReason),
                ["SmartStore.Core.Domain.Customers.WalletPostingReason"] = nameof(WalletPostingReason),
                //["SmartStore.AmazonPay.Services.AmazonPayAuthorizeMethod"] = nameof(AmazonPayAuthorizeMethod),
                ["SmartStore.Core.Domain.Catalog.PriceDisplayStyle"] = nameof(PriceDisplayStyle),
                ["SmartStore.Core.Domain.Customers.CustomerLoginType"] = nameof(CustomerLoginType),
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
                ["SmartStore.Core.Domain.Catalog.ProductVisibility"] = nameof(ProductVisibility),
                ["SmartStore.Rules.RuleScope"] = nameof(RuleScope),
                ["SmartStore.Core.Domain.Catalog.ProductCondition"] = nameof(ProductCondition),
                ["SmartStore.Core.Search.IndexingStatus"] = nameof(IndexingStatus),
                //["SmartStore.PayPal.Services.PayPalPromotion"] = nameof(PayPalPromotion),
                //["SmartStore.Core.Domain.Blogs.PreviewDisplayType"] = nameof(PreviewDisplayType),
                ["SmartStore.Core.Domain.Directory.DeliveryTimesPresentation"] = nameof(DeliveryTimesPresentation),
                ["SmartStore.Core.Domain.Catalog.AttributeChoiceBehaviour"] = nameof(AttributeChoiceBehaviour)
            };
        }
    }
}
