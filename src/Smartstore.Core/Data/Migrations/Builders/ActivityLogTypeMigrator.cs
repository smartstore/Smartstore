using Smartstore.Core.Localization;
using Smartstore.Core.Logging;

namespace Smartstore.Core.Data.Migrations
{
    internal class ActivityLogTypeMigrator
    {
        private readonly SmartDbContext _db;

        public ActivityLogTypeMigrator(SmartDbContext db)
        {
            _db = Guard.NotNull(db, nameof(db));
        }

        public async Task InsertActivityLogTypeAsync(string systemKeyword, string enName, string deName)
        {
            Guard.NotEmpty(systemKeyword, nameof(systemKeyword));
            Guard.NotEmpty(enName, nameof(enName));
            Guard.NotEmpty(deName, nameof(deName));

            if (!await _db.ActivityLogTypes.AnyAsync(x => x.SystemKeyword == systemKeyword))
            {
                var language = await GetDefaultAdminLanguageAsync();

                _db.ActivityLogTypes.Add(new ActivityLogType
                {
                    Enabled = true,
                    SystemKeyword = systemKeyword,
                    Name = (language.UniqueSeoCode.EqualsNoCase("de") ? deName : enName)
                });

                await _db.SaveChangesAsync();
            }
        }

        private async Task<Language> GetDefaultAdminLanguageAsync()
        {
            const string settingKey = "LocalizationSettings.DefaultAdminLanguageId";

            var defaultAdminLanguageSetting =
                await _db.Settings.FirstOrDefaultAsync(x => x.Name == settingKey && x.StoreId == 0) ??
                await _db.Settings.FirstOrDefaultAsync(x => x.Name == settingKey);

            if (defaultAdminLanguageSetting != null)
            {
                var defaultAdminLanguageId = defaultAdminLanguageSetting.Value.ToInt();
                if (defaultAdminLanguageId != 0)
                {
                    var language = await _db.Languages.FindByIdAsync(defaultAdminLanguageId);
                    if (language != null)
                        return language;
                }
            }

            return await _db.Languages.FirstOrDefaultAsync();
        }
    }
}
