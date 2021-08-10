using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateEnumResources(context, cancelToken);
        }

        public static void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            #region General

            // TODO: (core) Delete all Telerik language resources (???)

            builder.AddOrUpdate("Common.Exit", "Exit", "Beenden");
            builder.AddOrUpdate("Common.Empty", "Empty", "Leer");
            builder.AddOrUpdate("Admin.Common.SaveChanges", "Save changes", "Änderungen speichern");
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
                "There were no categories found.",
                "Es wurden keine Warengruppen gefunden.");

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

            #region Fixes

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
                    var lastPart = key.Substring(lastDotIndex + 1);

                    // Trim "Enums." and last Part
                    key = key.Substring(6, lastDotIndex - 6);

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
                //["SmartStore.Core.Domain.Forums.EditorType"] = nameof(EditorType),
                //["SmartStore.Core.Domain.Forums.ForumTopicType"] = nameof(ForumTopicType),
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
                ["SmartStore.Core.Domain.DataExchange.ImportFileType"] = nameof(ImportFileType),
                ["SmartStore.Core.Domain.Orders.CheckoutNewsLetterSubscription"] = nameof(CheckoutNewsLetterSubscription),
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
                //["SmartStore.Core.Domain.Forums.ForumTopicSorting"] = nameof(ForumTopicSorting),
                //["SmartStore.Core.Domain.Forums.ForumDateFilter"] = nameof(ForumDateFilter),
                ["SmartStore.Core.Domain.Catalog.PriceDisplayStyle"] = nameof(PriceDisplayStyle),
                ["SmartStore.Core.Domain.DataExchange.RelatedEntityType"] = nameof(RelatedEntityType),
                ["SmartStore.Core.Domain.Customers.CustomerLoginType"] = nameof(CustomerLoginType),
                //["SmartStore.PageBuilder.Blocks.CategoryDisplayType"] = nameof(CategoryDisplayType),
                //["SmartStore.PageBuilder.Blocks.CategoryPickingType"] = nameof(CategoryPickingType),
                //["SmartStore.PageBuilder.Blocks.IconAlignment"] = nameof(IconAlignment),
                //["SmartStore.PageBuilder.Blocks.IconDisplayType"] = nameof(IconDisplayType),
                //["SmartStore.PageBuilder.Blocks.ProductListDisplayType"] = nameof(ProductListDisplayType),
                //["SmartStore.PageBuilder.Blocks.ProductPickingType"] = nameof(ProductPickingType),
                //["SmartStore.PageBuilder.Models.MegaSizeTypes"] = nameof(MegaSizeTypes),
                //["SmartStore.PageBuilder.StoryTemplateGroup"] = nameof(StoryTemplateGroup),
                //["SmartStore.PageBuilder.Blocks.BrandListDisplayType"] = nameof(BrandListDisplayType),
                //["SmartStore.PageBuilder.Blocks.ButtonAlignment"] = nameof(ButtonAlignment),
                //["SmartStore.PageBuilder.Blocks.ButtonIconAlignment"] = nameof(ButtonIconAlignment),
                //["SmartStore.PageBuilder.Blocks.TitleDisplayType"] = nameof(TitleDisplayType),
                //["SmartStore.PageBuilder.Models.BoxImagePlacement"] = nameof(BoxImagePlacement),
                //["SmartStore.PageBuilder.Models.GradientRepeat"] = nameof(GradientRepeat),
                //["SmartStore.PageBuilder.Blocks.GalleryStyle"] = nameof(GalleryStyle),
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
