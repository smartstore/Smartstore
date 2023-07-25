using System.Globalization;
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
                    db.Settings.Add(new Setting
                    {
                        Name = "PrivacySettings.CookieConsentRequirement",
                        Value = setting.Value.ToBool() ? CookieConsentRequirement.RequiredInEUCountriesOnly.ToString() : CookieConsentRequirement.NeverRequired.ToString(),
                        StoreId = setting.StoreId
                    });
                }
            }

            await db.SaveChangesAsync(cancelToken);

            await MigrateOfflinePaymentDescriptions(db, cancelToken);
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
                "Payment.PayingFailed");
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
        /// Migrates the import profile column mapping of localized properties (if any) from language SEO code to language culture (see issue #531).
        /// </summary>
        private static async Task MigrateImportProfileColumnMapping(SmartDbContext db, CancellationToken cancelToken)
        {
            var profiles = await db.ImportProfiles
                .Where(x => x.ColumnMapping.Length > 3)
                .ToListAsync(cancelToken);
            if (profiles.Count == 0)
            {
                return;
            }

            try
            {
                var mapConverter = new ColumnMapConverter();
                var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Where(x => !x.Equals(CultureInfo.InvariantCulture) && !x.IsNeutralCulture && x.Name.HasValue())
                    .ToArray();

                foreach (var profile in profiles)
                {
                    var storedMap = mapConverter.ConvertFrom<ColumnMap>(profile.ColumnMapping);
                    if (storedMap == null)
                        continue;

                    var map = new ColumnMap();

                    foreach (var mapping in storedMap.Mappings.Select(x => x.Value))
                    {
                        var mapped = false;

                        if (ColumnMap.ParseSourceName(mapping.SourceName, out string name, out string index)
                            && name.HasValue()
                            && index.HasValue()
                            && index.Length == 2)
                        {
                            var culture = $"{index.ToLowerInvariant()}-{index.ToUpperInvariant()}";
                            if (allCultures.Any(x => x.Name == culture))
                            {
                                // TODO: migrate "MappedName" too.
                                map.AddMapping(name, culture, mapping.MappedName, mapping.Default);
                                mapped = true;
                            }
                        }

                        if (!mapped)
                        {
                            //...
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}