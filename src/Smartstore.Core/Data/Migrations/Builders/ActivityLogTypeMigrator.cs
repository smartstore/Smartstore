using Smartstore.Core.Localization;
using Smartstore.Core.Logging;

namespace Smartstore.Core.Data.Migrations
{
    internal class ActivityLogTypeMigrator(SmartDbContext db)
    {
        const int BatchSize = 1000;

        private readonly SmartDbContext _db = Guard.NotNull(db);

        /// <summary>
        /// Consolidates and deletes duplicate activity log types.
        /// </summary>
        /// <returns>Number of deleted activity log types.</returns>
        public async Task<int> DeleteDuplicateActivityLogTypeAsync(CancellationToken cancelToken = default)
        {
            var numDeleted = 0;
            var duplicateGroups = await _db.ActivityLogTypes
                .AsNoTracking()
                .GroupBy(t => new { t.SystemKeyword, t.Enabled })
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    g.Key.SystemKeyword,
                    g.Key.Enabled,
                    KeepId = g.OrderBy(t => t.Id)
                        .Select(t => t.Id)
                        .First(),
                    DuplicateIds = g.OrderBy(t => t.Id)
                        .Select(t => t.Id)
                        .Skip(1)
                        .ToList()
                })
                .ToListAsync(cancelToken);

            if (duplicateGroups.Count == 0)
            {
                return 0;
            }

            await using var tx = await _db.Database.BeginTransactionAsync(cancelToken);

            try
            {
                foreach (var group in duplicateGroups)
                {
                    foreach (var chunk in group.DuplicateIds.Chunk(BatchSize))
                    {
                        await _db.ActivityLogs
                            .Where(x => chunk.Contains(x.ActivityLogTypeId))
                            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ActivityLogTypeId, group.KeepId), cancelToken);

                        numDeleted += await _db.ActivityLogTypes
                            .Where(x => chunk.Contains(x.Id))
                            .ExecuteDeleteAsync(cancelToken);
                    }
                }

                await tx.CommitAsync(cancelToken);
            }
            catch
            {
                await tx.RollbackAsync(cancelToken);
                throw;
            }

            return numDeleted;
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
