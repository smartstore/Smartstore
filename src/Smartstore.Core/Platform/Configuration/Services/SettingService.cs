using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.Core.Data;
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
        const string ClassCacheKeyPattern = "settings:{0}.{1}";

        // 0 = Setting.Name, 1 = StoreId
        const string RawCacheKeyPattern = "rawsettings:{0}.{1}";

        internal readonly static TimeSpan DefaultExpiry = TimeSpan.FromHours(8);

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly DbSet<Setting> _setSettings;

        public SettingService(ICacheManager cache, SmartDbContext db)
        {
            _cache = cache;
            _db = db;
            _setSettings = _db.Settings;
        }

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
                    return (index == -1 ? x.Name : x.Name.Substring(0, index)).ToLowerInvariant();
                })
                .Distinct()
                .ToArray();

            foreach (var prefix in prefixes)
            {
                var numClasses = await _cache.RemoveByPatternAsync(BuildCacheKeyForClassAccess(prefix, "*"));
                var numRaw = await _cache.RemoveByPatternAsync(BuildCacheKeyForRawAccess(prefix, "*"));
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
        public virtual async Task<T> GetSettingByKeyAsync<T>(string key, T defaultValue = default, int storeId = 0, bool doFallback = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var cachedSetting = await GetCachedSettingAsync(key, storeId);

            if (doFallback && cachedSetting.Id == 0 && storeId > 0)
            {
                cachedSetting = await GetCachedSettingAsync(key, 0);
            }

            return cachedSetting.Id > 0
                ? cachedSetting.Value.Convert<T>()
                : defaultValue;
        }

        private async Task<CachedSetting> GetCachedSettingAsync(string key, int storeId)
        {
            var cacheKey = BuildCacheKeyForRawAccess(key, storeId);

            var cachedSetting = await _cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(DefaultExpiry);
                
                var setting = await _setSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Name == key && x.StoreId == storeId);
                return new CachedSetting
                {
                    Id = setting?.Id ?? 0,
                    StoreId = storeId,
                    Value = setting?.Value
                };
            }, independent: true, allowRecursion: true);

            return cachedSetting;
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
            Guard.NotEmpty(key, nameof(key));

            key = key.ToLowerInvariant();

            var str = value.Convert<string>();
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

            var prefix = rootKey.EnsureEndsWith(".");

            var stubs = await _setSettings
                .AsNoTracking()
                .Where(x => x.Name.StartsWith(rootKey))
                .Select(x => new Setting { Id = x.Id })
                .ToListAsync();

            //var stubs = await _setSettings
            //    .Where(x => x.Name.StartsWith(rootKey))
            //    .ToListAsync();

            _setSettings.RemoveRange(stubs);

            return stubs.Count;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> RemoveSettingAsync(string key, int storeId = 0)
        {
            if (key.HasValue())
            {
                key = key.Trim().ToLowerInvariant();

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
            return ClassCacheKeyPattern.FormatInvariant(settingsType.Name.ToLowerInvariant(), storeId.ToString(CultureInfo.InvariantCulture));
        }

        internal static string BuildCacheKeyForClassAccess(string prefix, string suffix)
        {
            return ClassCacheKeyPattern.FormatInvariant(prefix.ToLowerInvariant(), suffix);
        }

        internal static string BuildCacheKeyForRawAccess(string prefix, int storeId)
        {
            return RawCacheKeyPattern.FormatInvariant(prefix.ToLowerInvariant(), storeId.ToString(CultureInfo.InvariantCulture));
        }

        internal static string BuildCacheKeyForRawAccess(string prefix, string suffix)
        {
            return RawCacheKeyPattern.FormatInvariant(prefix.ToLowerInvariant(), suffix);
        }

        #endregion
    }
}