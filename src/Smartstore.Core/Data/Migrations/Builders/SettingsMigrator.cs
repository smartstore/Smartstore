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
            Guard.NotNull(db, nameof(db));

            _db = db;
            _settings = _db.Settings;
        }

        public async Task MigrateAsync(IEnumerable<SettingEntry> entries)
        {
            Guard.NotNull(entries, nameof(entries));

            if (!entries.Any())
                return;

            var toDelete = new List<Setting>();
            var toAdd = new List<Setting>();

            using var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Essential);

            // First perform DELETE actions
            foreach (var entry in entries.Where(x => x.Value == null))
            {
                bool isPattern = entry.KeyIsGroup;
                if (!await HasSettingsAsync(entry.Key, isPattern))
                    continue; // nothing to delete

                var dbSettings = await GetSettingsAsync(entry.Key, isPattern);
                _settings.RemoveRange(dbSettings);
            }

            await _db.SaveChangesAsync();

            // Then perform ADD actions
            foreach (var entry in entries.Where(x => x.Value.HasValue()))
            {
                var existing = toAdd.FirstOrDefault(x => x.Name.Equals(entry.Key, StringComparison.InvariantCultureIgnoreCase));
                if (existing != null)
                {
                    existing.Value = entry.Value;
                    continue;
                }

                if (await HasSettingsAsync(entry.Key, false))
                    continue; // skip existing (we don't perform updates)

                _settings.Add(new Setting
                {
                    Name = entry.Key,
                    Value = entry.Value,
                    StoreId = 0
                });
            }

            await _db.SaveChangesAsync();
        }

        private Task<bool> HasSettingsAsync(string key, bool isPattern = false)
            => BuildQuery(key, isPattern).AnyAsync();

        private Task<List<Setting>> GetSettingsAsync(string key, bool isPattern = false)
            => BuildQuery(key, isPattern).ToListAsync();

        private IQueryable<Setting> BuildQuery(string key, bool isPattern = false)
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

            return query;
        }
    }
}
