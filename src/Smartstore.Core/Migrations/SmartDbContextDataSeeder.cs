using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Identity;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

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
                    db.Settings.Remove(setting);
                }
            }

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
                "Enums.BackorderMode.AllowQtyBelow0AndNotifyCustomer");

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