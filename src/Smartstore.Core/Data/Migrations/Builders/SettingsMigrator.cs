using Smartstore.Core.Configuration;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Data.Migrations
{
    internal class SettingsMigrator
    {
        private readonly SmartDbContext _db;
        private readonly DbSet<Setting> _settings;

        public SettingsMigrator(SmartDbContext db)
        {
            Guard.NotNull(db);

            _db = db;
            _settings = _db.Settings;
        }

        public async Task MigrateAsync(IEnumerable<SettingEntry> entries)
        {
            Guard.NotNull(entries);

            if (!entries.Any())
                return;

            var toAdd = new List<Setting>();

            using var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Essential);

            // First perform DELETE operations
            var operations = entries.Where(x => x.Operation == SettingEntryOperation.Delete).ToList();
            if (operations.Count > 0)
            {
                foreach (var entry in operations)
                {
                    bool isPattern = entry.KeyIsGroup;
                    if (!await HasSettingsAsync(entry.Key, entry.StoreId, isPattern))
                    {
                        continue; // nothing to delete
                    }

                    var dbSettings = await GetSettingsAsync(entry.Key, entry.StoreId, isPattern);
                    _settings.RemoveRange(dbSettings);
                }

                await _db.SaveChangesAsync();
            }


            // Then perform ADD operations
            operations = entries.Where(x => x.Operation == SettingEntryOperation.Add).ToList();
            if (operations.Count > 0)
            {
                foreach (var entry in operations)
                {
                    if (entry.Value == null)
                    {
                        continue;
                    }

                    // TODO: (mc) toAdd never gets filled. Hmmm, investigate.
                    var existing = toAdd.FirstOrDefault(x => x.Name.Equals(entry.Key, StringComparison.InvariantCultureIgnoreCase));
                    if (existing != null)
                    {
                        existing.Value = entry.Value;
                        continue;
                    }

                    if (await HasSettingsAsync(entry.Key, entry.StoreId, false))
                    {
                        continue; // skip existing (we don't perform updates here)
                    }

                    _settings.Add(new Setting
                    {
                        Name = entry.Key,
                        Value = entry.Value,
                        StoreId = entry.StoreId ?? 0
                    });
                }

                await _db.SaveChangesAsync();
            }

            // Now perform UPDATE operations
            operations = entries.Where(x => x.Operation == SettingEntryOperation.Update).ToList();
            if (operations.Count > 0)
            {
                foreach (var entry in entries.Where(x => x.Operation == SettingEntryOperation.Update))
                {
                    var existingSettings = await GetSettingsAsync(entry.Key, entry.StoreId, false);

                    foreach (var setting in existingSettings)
                    {
                        if (entry.DefaultValue == null || entry.DefaultValue != setting.Value)
                        {
                            setting.Value = entry.Value;
                        }
                    }
                }

                await _db.SaveChangesAsync();
            }
        }

        private Task<bool> HasSettingsAsync(string key, int? storeId, bool isPattern = false)
            => BuildQuery(key, storeId, isPattern).AnyAsync();

        private Task<List<Setting>> GetSettingsAsync(string key, int? storeId, bool isPattern = false)
            => BuildQuery(key, storeId, isPattern).ToListAsync();

        private IQueryable<Setting> BuildQuery(string key, int? storeId, bool isPattern = false)
        {
            var query = _settings.AsQueryable();
            if (isPattern)
            {
                query = query.Where(x => x.Name.StartsWith(key));
            }
            else
            {
                query = query.Where(x => x.Name.Equals(key));
            }

            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            return query;
        }
    }
}
