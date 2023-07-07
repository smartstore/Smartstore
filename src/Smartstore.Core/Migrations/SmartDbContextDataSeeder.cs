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
    }
}