using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
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
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.Settings.Order.GiftCards_Activated")
                .Value("de", "Geschenkgutschein ist aktiviert, wenn Auftragsstatus...");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.GiftCards_Activated.Hint")
                .Value("de", "Legt den Auftragsstatus einer Bestellung fest, bei dem in der Bestellung enthaltene Geschenkgutscheine automatisch aktiviert werden.");
        }

        /// <summary>
        /// Migrates old payment description (xyzSettings.DescriptionText) of offline payment methods.
        /// </summary>
        private static async Task MigrateOfflinePaymentDescriptions(SmartDbContext db, CancellationToken cancelToken)
        {
            var masterLanguageId = await db.Languages
                .Where(x => x.Published)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancelToken);
            if (masterLanguageId == 0)
            {
                return;
            }

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

            var systemNames = names.Keys.ToArray();
            var settingNames = names.Values.ToArray();

            var paymentMethods = (await db.PaymentMethods
                .Where(x => systemNames.Contains(x.PaymentMethodSystemName))
                .ToListAsync(cancelToken))
                .ToDictionarySafe(x => x.PaymentMethodSystemName, x => x, StringComparer.OrdinalIgnoreCase);

            var descriptionSettings = (await db.Settings
                .AsNoTracking()
                .Where(x => settingNames.Contains(x.Name) && x.StoreId == 0)
                .ToListAsync(cancelToken))
                .ToDictionarySafe(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

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

            // Delete old xyzSettings.DescriptionText.
            await db.Settings
                .Where(x => settingNames.Contains(x.Name))
                .ExecuteDeleteAsync(cancelToken);
        }
    }
}