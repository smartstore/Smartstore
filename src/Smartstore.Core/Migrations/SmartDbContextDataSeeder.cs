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
                    db.Settings.Remove(setting);
                }
            }

            // Remove duplicate settings for PrivacySettings.CookieConsentRequirement
            var stores = await db.Stores.ToListAsync(cancelToken);
            var cookieConsentRequirementSettings = await db.Settings
                .Where(x => x.Name == "PrivacySettings.CookieConsentRequirement")
                .ToListAsync(cancelToken);

            if (cookieConsentRequirementSettings.Count > stores.Count)
            {
                foreach (var store in stores)
                {
                    var storeSpecificSettings = cookieConsentRequirementSettings
                        .Where(x => x.StoreId == store.Id)
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
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.Delete("Admin.Configuration.Settings.CustomerUser.Privacy.EnableCookieConsent.Hint");
        }
    }
}