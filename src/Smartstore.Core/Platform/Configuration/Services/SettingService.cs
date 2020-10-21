using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Z.EntityFramework.Plus;

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

        public override Task OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            // Indicate that we gonna handle this
            return Task.CompletedTask;
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Obtain distict prefixes from all changed settings,
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
        public virtual async Task<bool> SettingExistsAsync<T, TPropType>(
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            int storeId = 0)
            where T : ISettings, new()
        {
            Guard.NotNull(keySelector, nameof(keySelector));
            
            var propInfo = GetPropertyInfo(keySelector);
            var key = string.Concat(typeof(T).Name, ".", propInfo.Name);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Task<SaveSettingResult> SetSettingAsync<T>(string key, T value, int storeId = 0)
        {
            return SetSettingAsync(key, value, storeId, () => BuildCacheKeyForRawAccess(key, storeId));
        }

        private async Task<SaveSettingResult> SetSettingAsync<T>(string key, T value, int storeId, Func<string> cacheKeyCreator)
        {
            Guard.NotEmpty(key, nameof(key));

            var str = value.Convert<string>();
            var cacheKey = cacheKeyCreator();
            var insert = false;

            if ((await _cache.TryGetAsync<CachedSetting>(cacheKey)).Out(out var cachedSetting) && cachedSetting.Id > 0)
            {
                var setting = await _setSettings.FindByIdAsync(cachedSetting.Id);
                if (setting != null)
                {
                    // Update
                    if (setting.Value != str)
                    {
                        setting.Value = str;
                        return SaveSettingResult.Modified;
                    }
                }
                else
                {
                    insert = true;
                }
            }
            else
            {
                insert = true;
            }

            if (insert)
            {
                // Insert
                var setting = new Setting
                {
                    Name = key.ToLowerInvariant(),
                    Value = str,
                    StoreId = storeId
                };

                _setSettings.Add(setting);
                return SaveSettingResult.Inserted;
            }

            return SaveSettingResult.Unchanged;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveSettingsAsync<T>(T settings, int storeId = 0) where T : ISettings, new()
        {
            Guard.NotNull(settings, nameof(settings));

            var hasChanges = false;
            var settingType = typeof(T);
            var prefix = settingType.Name;

            /* We do not clear cache after each setting update.
				* This behavior can increase performance because cached settings will not be cleared 
				* and loaded from database after each update */
            foreach (var prop in FastProperty.GetProperties(settingType).Values)
            {
                // get properties we can read and write to
                if (!prop.IsPublicSettable)
                    continue;

                var converter = TypeConverterFactory.GetConverter(prop.Property.PropertyType);
                if (converter == null || !converter.CanConvertFrom(typeof(string)))
                    continue;

                string key = prefix + "." + prop.Name;
                // Duck typing is not supported in C#. That's why we're using dynamic type
                dynamic currentValue = prop.GetValue(settings);

                if (await SetSettingAsync(key, currentValue, storeId) > SaveSettingResult.Unchanged)
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <inheritdoc/>
        public virtual async Task<SaveSettingResult> SaveSettingAsync<T, TPropType>(T settings, Expression<Func<T, TPropType>> keySelector, int storeId = 0)
            where T : ISettings, new()
        {
            var propInfo = GetPropertyInfo(keySelector);
            var key = string.Concat(typeof(T).Name, ".", propInfo.Name);

            // Duck typing is not supported in C#. That's why we're using dynamic type.
            var fastProp = FastProperty.GetProperty(propInfo, PropertyCachingStrategy.EagerCached);
            dynamic currentValue = fastProp.GetValue(settings);

            return await SetSettingAsync(key, currentValue, storeId);
        }

        /// <inheritdoc/>
        public virtual async Task<SaveSettingResult> UpdateSettingAsync<T, TPropType>(
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            bool overrideForStore,
            int storeId = 0) where T : ISettings, new()
        {
            if (overrideForStore || storeId == 0)
            {
                return await SaveSettingAsync(settings, keySelector, storeId);
            }
            else if (storeId > 0 && await DeleteSettingAsync(settings, keySelector, storeId))
            {
                return SaveSettingResult.Deleted;
            }

            return SaveSettingResult.Unchanged;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<int> DeleteSettingsAsync<T>() where T : ISettings, new()
        {
            return await DeleteSettingsAsync(typeof(T).Name);
        }

        /// <inheritdoc/>
        public virtual async Task<int> DeleteSettingsAsync(string rootKey)
        {
            if (rootKey.IsEmpty())
                return 0;

            var prefix = rootKey.EnsureEndsWith(".");

            //var stubs = await _setSettings.AsNoTracking()
            //    .Where(x => x.Name.StartsWith(rootKey))
            //    .Select(x => new Setting { Id = x.Id })
            //    .ToListAsync();

            var stubs = await _setSettings
                .Where(x => x.Name.StartsWith(rootKey))
                .ToListAsync();

            _setSettings.RemoveRange(stubs);

            return stubs.Count;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteSettingAsync<T, TPropType>(T settings, Expression<Func<T, TPropType>> keySelector, int storeId = 0) 
            where T : ISettings, new()
        {
            var propInfo = GetPropertyInfo(keySelector);
            var key = string.Concat(typeof(T).Name, ".", propInfo.Name);

            return await DeleteSettingAsync(key, storeId);
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteSettingAsync(string key, int storeId = 0)
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

        protected virtual PropertyInfo GetPropertyInfo<T, TPropType>(Expression<Func<T, TPropType>> keySelector)
        {
            var member = keySelector.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");
            }

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
            {
                throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");
            }

            return propInfo;
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