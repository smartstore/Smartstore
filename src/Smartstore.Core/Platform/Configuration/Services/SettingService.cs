using System.Text;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Configuration
{
    [Serializable]
    public class CachedSetting
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public int StoreId { get; set; }
    }

    [Important]
    public partial class SettingService : AsyncDbSaveHook<Setting>, ISettingService
    {
        // 0 = SettingType, 1 = StoreId
        private readonly static CompositeFormat ClassCacheKeyPattern = CompositeFormat.Parse("settings:{0}.{1}");

        // 0 = Setting.Name, 1 = StoreId
        private readonly static CompositeFormat RawCacheKeyPattern = CompositeFormat.Parse("rawsettings:{0}.{1}");

        internal readonly static TimeSpan DefaultExpiry = TimeSpan.FromHours(8);

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly DbSet<Setting> _setSettings;
        private readonly IActivityLogger _activityLogger;

        public SettingService(ICacheManager cache, SmartDbContext db, IActivityLogger activityLogger)
        {
            _cache = cache;
            _db = db;
            _setSettings = _db.Settings;
            _activityLogger = activityLogger;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            // Indicate that we gonna handle this
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Obtain distinct prefixes from all changed settings,
            // e.g.: 'catalogsettings.showgtin' > 'catalogsettings'
            var prefixes = entries
                .Select(x => x.Entity)
                .OfType<Setting>()
                .Select(x =>
                {
                    var index = x.Name.LastIndexOf('.');
                    return (index == -1 ? x.Name : x.Name[..index]);
                })
                .Distinct()
                .ToArray();

            foreach (var prefix in prefixes)
            {
                var numClasses = await _cache.RemoveByPatternAsync(BuildCacheKeyForClassAccess(prefix, "*"));
                var numRaw = await _cache.RemoveByPatternAsync(BuildCacheKeyForRawAccess(prefix, "*"));
            }

            // Log activity.
            var updatedEntities = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Modified)
                .Select(x => x.Entity)
                .OfType<Setting>()
                .ToList();

            if (updatedEntities.Any())
            {
                string comment = T("ActivityLog.EditSettings");

                updatedEntities.Each(x => _activityLogger.LogActivity(KnownActivityLogTypes.EditSettings, comment, x.Name, x.Value));
                await _db.SaveChangesAsync(cancelToken);
            }
        }

        #endregion

        #region ISettingService

        /// <inheritdoc/>
        public virtual async Task<bool> SettingExistsAsync(string key, int storeId = 0)
        {
            Guard.NotEmpty(key, nameof(key));

            return await _setSettings.AnyAsync(x => x.Name == key && x.StoreId == storeId);
        }

        /// <inheritdoc/>
        public virtual T GetSettingByKey<T>(string key, T defaultValue = default, int storeId = 0, bool doFallback = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var cachedSetting = GetCachedSettingInternal(key, storeId, false).Await();

            if (doFallback && cachedSetting.Id == 0 && storeId > 0)
            {
                cachedSetting = GetCachedSettingInternal(key, 0, false).Await();
            }

            return cachedSetting.Id > 0
                ? cachedSetting.Value.Convert<T>()
                : defaultValue;
        }

        /// <inheritdoc/>
        public virtual async Task<T> GetSettingByKeyAsync<T>(string key, T defaultValue = default, int storeId = 0, bool doFallback = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var cachedSetting = await GetCachedSettingInternal(key, storeId, true);

            if (doFallback && cachedSetting.Id == 0 && storeId > 0)
            {
                cachedSetting = await GetCachedSettingInternal(key, 0, true);
            }

            return cachedSetting.Id > 0
                ? cachedSetting.Value.Convert<T>()
                : defaultValue;
        }

        private async Task<CachedSetting> GetCachedSettingInternal(string key, int storeId, bool async)
        {
            var cacheKey = BuildCacheKeyForRawAccess(key, storeId);
            return await _cache.GetAsync(cacheKey, GetEntry, independent: true, allowRecursion: true);

            async Task<CachedSetting> GetEntry(CacheEntryOptions o)
            {
                o.ExpiresIn(DefaultExpiry);

                var setting = async
                    ? await _setSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Name == key && x.StoreId == storeId)
                    : _setSettings.AsNoTracking().FirstOrDefault(x => x.Name == key && x.StoreId == storeId);

                return new CachedSetting
                {
                    Id = setting?.Id ?? 0,
                    StoreId = storeId,
                    Value = setting?.Value
                };
            }
        }

        /// <inheritdoc/>
        public virtual async Task<Setting> GetSettingEntityByKeyAsync(string key, int storeId = 0)
        {
            Guard.NotEmpty(key, nameof(key));

            var query = _setSettings.Where(x => x.Name == key);

            if (storeId > 0)
            {
                query = query.Where(x => x.StoreId == storeId || x.StoreId == 0).OrderByDescending(x => x.StoreId);
            }
            else
            {
                query = query.Where(x => x.StoreId == 0);
            }

            return await query.FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<ApplySettingResult> ApplySettingAsync<T>(string key, T value, int storeId = 0)
        {
            Guard.NotEmpty(key);

            var str = value.Convert<string>() ?? string.Empty;
            var setting = await _setSettings.FirstOrDefaultAsync(x => x.Name == key && x.StoreId == storeId);

            if (setting == null)
            {
                // Insert
                setting = new Setting
                {
                    Name = key,
                    Value = str,
                    StoreId = storeId
                };

                _setSettings.Add(setting);
                return ApplySettingResult.Inserted;
            }
            else
            {
                // Update
                if (setting.Value != str)
                {
                    setting.Value = str;
                    return ApplySettingResult.Modified;
                }
            }

            return ApplySettingResult.Unchanged;
        }

        /// <inheritdoc/>
        public virtual async Task<int> RemoveSettingsAsync(string rootKey)
        {
            if (rootKey.IsEmpty())
                return 0;

            var prefix = rootKey.EnsureEndsWith('.');

            var stubs = await _setSettings
                .AsNoTracking()
                .Where(x => x.Name.StartsWith(rootKey))
                .Select(x => new Setting { Id = x.Id, Name = x.Name, StoreId = x.StoreId })
                .ToListAsync();

            _setSettings.RemoveRange(stubs);

            return stubs.Count;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> RemoveSettingAsync(string key, int storeId = 0)
        {
            if (key.HasValue())
            {
                key = key.Trim();

                var setting = await (
                    from s in _setSettings
                    where s.StoreId == storeId && s.Name == key
                    select s).FirstOrDefaultAsync();

                if (setting != null)
                {
                    _setSettings.Remove(setting);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Utils

        internal static string BuildCacheKeyForClassAccess(Type settingsType, int storeId)
        {
            return ClassCacheKeyPattern.FormatInvariant(settingsType.Name.ToLowerInvariant(), storeId.ToStringInvariant());
        }

        internal static string BuildCacheKeyForClassAccess(string prefix, string suffix)
        {
            return ClassCacheKeyPattern.FormatInvariant(prefix.ToLowerInvariant(), suffix);
        }

        internal static string BuildCacheKeyForRawAccess(string prefix, int storeId)
        {
            return RawCacheKeyPattern.FormatInvariant(prefix.ToLowerInvariant(), storeId.ToStringInvariant());
        }

        internal static string BuildCacheKeyForRawAccess(string prefix, string suffix)
        {
            return RawCacheKeyPattern.FormatInvariant(prefix.ToLowerInvariant(), suffix);
        }

        #endregion
    }
}