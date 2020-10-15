using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Z.EntityFramework.Plus;

namespace Smartstore.Core.Configuration
{
    public partial class SettingService : ScopedServiceBase, ISettingService, IDbSaveHook
    {
        private const string SETTINGS_ALL_KEY = "setting:all";

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

        public Task OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cencelToken)
            => throw new NotSupportedException();

        public Task OnAfterSaveAsync(IHookedEntity entry, CancellationToken cencelToken)
        {
            if (!typeof(Setting).IsAssignableFrom(entry.EntityType))
            {
                throw new NotSupportedException();
            }

            // Handle cache invalidation in OnAfterSaveCompletedAsync
            return Task.CompletedTask;
        }

        public Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cencelToken)
            => Task.CompletedTask;

        /// <summary>
        /// Called after all entities in the current unit of work has been handled after saving changes to the database
        /// </summary>
        public Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cencelToken)
        {
            HasChanges = true;
            ClearCache();

            return Task.CompletedTask;
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
            var propInfo = GetPropertyInfo(keySelector);
            var key = string.Concat(typeof(T).Name, ".", propInfo.Name);

            string setting = await GetSettingByKeyAsync<string>(key, storeId: storeId);
            return setting != null;
        }

        /// <inheritdoc/>
        public virtual async Task<T> GetSettingByKeyAsync<T>(
            string key,
            T defaultValue = default,
            int storeId = 0,
            bool loadSharedValueIfNotFound = false)
        {
            Guard.NotEmpty(key, nameof(key));

            var settings = await GetAllCachedSettingsAsync();

            var cacheKey = CreateCacheKey(key, storeId);

            if (settings.TryGetValue(cacheKey, out CachedSetting cachedSetting))
            {
                return cachedSetting.Value.Convert<T>();
            }

            // fallback to shared (storeId = 0) if desired
            if (storeId > 0 && loadSharedValueIfNotFound)
            {
                cacheKey = CreateCacheKey(key, 0);
                if (settings.TryGetValue(cacheKey, out cachedSetting))
                {
                    return cachedSetting.Value.Convert<T>();
                }
            }

            return defaultValue;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LoadSettings<T>(int storeId = 0) where T : ISettings, new()
        {
            return (T)LoadSettingsCore(GetAllCachedSettings(), typeof(T), storeId);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> LoadSettingsAsync<T>(int storeId = 0) where T : ISettings, new()
        {
            return (T)LoadSettingsCore(await GetAllCachedSettingsAsync(), typeof(T), storeId);
        }

        /// <inheritdoc/>
        public ISettings LoadSettings(Type settingType, int storeId = 0)
        {
            Guard.NotNull(settingType, nameof(settingType));
            Guard.HasDefaultConstructor(settingType);

            if (!typeof(ISettings).IsAssignableFrom(settingType))
            {
                throw new ArgumentException($"The type to load settings for must be a subclass of the '{typeof(ISettings).FullName}' interface", nameof(settingType));
            }

            return LoadSettingsCore(GetAllCachedSettings(), settingType, storeId);
        }

        /// <inheritdoc/>
        public async Task<ISettings> LoadSettingsAsync(Type settingType, int storeId = 0)
        {
            Guard.NotNull(settingType, nameof(settingType));
            Guard.HasDefaultConstructor(settingType);

            if (!typeof(ISettings).IsAssignableFrom(settingType))
            {
                throw new ArgumentException($"The type to load settings for must be a subclass of the '{typeof(ISettings).FullName}' interface", nameof(settingType));
            }

            return LoadSettingsCore(await GetAllCachedSettingsAsync(), settingType, storeId);
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
        public virtual async Task<SaveSettingResult> SetSettingAsync<T>(string key, T value, int storeId = 0)
        {
            Guard.NotEmpty(key, nameof(key));

            var str = value.Convert<string>();
            var allSettings = await GetAllCachedSettingsAsync();
            var cacheKey = CreateCacheKey(key, storeId);
            var insert = false;

            if (allSettings.TryGetValue(cacheKey, out CachedSetting cachedSetting))
            {
                var setting = await _setSettings.FindByIdAsync(cachedSetting.Id);
                if (setting != null)
                {
                    // Update
                    if (setting.Value != str)
                    {
                        setting.Value = str;
                        HasChanges = true;
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
                HasChanges = true;
                return SaveSettingResult.Inserted;
            }

            return SaveSettingResult.Unchanged;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> SaveSettingsAsync<T>(T settings, int storeId = 0) where T : ISettings, new()
        {
            Guard.NotNull(settings, nameof(settings));

            using (BeginScope(clearCache: true))
            {
                var hasChanges = false;
                var modifiedProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
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

                    if (await SetSettingAsync(key, currentValue ?? "", storeId) > SaveSettingResult.Unchanged)
                    {
                        hasChanges = true;
                    }
                }

                return hasChanges;
            }
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

            return await SetSettingAsync(key, currentValue ?? "", storeId);
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
        public virtual async Task<int> DeleteSettingsAsync<T>() where T : ISettings, new()
        {
            return await DeleteSettingsAsync(typeof(T).Name);
        }

        /// <inheritdoc/>
        public virtual async Task<int> DeleteSettingsAsync(string rootKey)
        {
            using (BeginScope())
            {
                if (rootKey.IsEmpty())
                    return 0;

                var prefix = rootKey.EnsureEndsWith(".");

                // TODO: (core) Test this out!
                var numDeleted = await _setSettings.Where(x => x.Name.StartsWith(prefix)).DeleteAsync();

                if (numDeleted > 0)
                    HasChanges = true;

                return numDeleted;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteSettingAsync<T, TPropType>(
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            int storeId = 0) where T : ISettings, new()
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
                    await _db.SaveChangesAsync();
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Utils

        protected virtual IDictionary<string, CachedSetting> GetAllCachedSettings()
        {
            return _cache.Get(SETTINGS_ALL_KEY, o =>
            {
                o.ExpiresIn(TimeSpan.FromHours(8));

                var settings = _setSettings.AsNoTracking().ApplySorting().ToList();
                var dictionary = new Dictionary<string, CachedSetting>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in settings)
                {
                    var settingKey = CreateCacheKey(s.Name, s.StoreId);

                    var cachedSetting = new CachedSetting
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Value = s.Value,
                        StoreId = s.StoreId
                    };

                    dictionary[settingKey] = cachedSetting;
                }

                return dictionary;
            }, independent: true, allowRecursion: true);
        }

        protected virtual async Task<IDictionary<string, CachedSetting>> GetAllCachedSettingsAsync()
        {
            return await _cache.GetAsync(SETTINGS_ALL_KEY, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(8));

                var query = _setSettings.AsNoTracking().ApplySorting();

                var settings = await query.ToListAsync();
                var dictionary = new Dictionary<string, CachedSetting>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in settings)
                {
                    var settingKey = CreateCacheKey(s.Name, s.StoreId);

                    var cachedSetting = new CachedSetting
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Value = s.Value,
                        StoreId = s.StoreId
                    };

                    dictionary[settingKey] = cachedSetting;
                }

                return dictionary;
            }, independent: true, allowRecursion: true);
        }

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

        private async Task<ISettings> LoadSettingsJsonAsync(Type settingType, int storeId = 0)
        {
            string key = settingType.Namespace + "." + settingType.Name;

            var settings = (ISettings)Activator.CreateInstance(settingType);

            var rawSetting = await GetSettingByKeyAsync<string>(key, storeId: storeId, loadSharedValueIfNotFound: true);
            if (rawSetting.HasValue())
            {
                JsonConvert.PopulateObject(rawSetting, settings);
            }

            return settings;
        }

        private async Task SaveSettingsJsonAsync(ISettings settings)
        {
            Type t = settings.GetType();
            string key = t.Namespace + "." + t.Name;
            var storeId = 0;

            var rawSettings = JsonConvert.SerializeObject(settings);
            await SetSettingAsync(key, rawSettings, storeId);
        }

        private async Task DeleteSettingsJsonAsync<T>()
        {
            Type t = typeof(T);
            string key = t.Namespace + "." + t.Name;

            // TODO: (core) no hook will run, 'cause notrack.
            await _setSettings
                .Where(x => x.Name == key)
                .DeleteAsync();
        }

        private async Task<IList<Setting>> GetAllSettingsAsync()
        {
            var settings = await _setSettings.ApplySorting().ToListAsync();
            return settings;
        }

        protected virtual ISettings LoadSettingsCore(
            IDictionary<string, CachedSetting> allSettings, 
            Type settingType, 
            int storeId = 0)
        {
            Guard.NotNull(allSettings, nameof(allSettings));
            Guard.NotNull(settingType, nameof(settingType));

            var instance = (ISettings)Activator.CreateInstance(settingType);
            var prefix = settingType.Name;

            foreach (var fastProp in FastProperty.GetProperties(settingType).Values)
            {
                var prop = fastProp.Property;

                // Get properties we can read and write to
                if (!prop.CanWrite)
                    continue;

                string key = prefix + "." + prop.Name;

                if (!allSettings.TryGetValue(CreateCacheKey(key, storeId), out var cachedSetting) && storeId > 0)
                {
                    // Fallback to shared (storeId = 0)
                    allSettings.TryGetValue(CreateCacheKey(key, 0), out cachedSetting);
                }

                string setting = cachedSetting?.Value;

                if (setting == null)
                {
                    if (fastProp.IsSequenceType)
                    {
                        if ((fastProp.GetValue(instance) as System.Collections.IEnumerable) != null)
                        {
                            // Instance of IEnumerable<> was already created, most likely in the constructor of the settings concrete class.
                            // In this case we shouldn't let the EnumerableConverter create a new instance but keep this one.
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                var converter = TypeConverterFactory.GetConverter(prop.PropertyType);

                if (converter == null || !converter.CanConvertFrom(typeof(string)))
                    continue;

                try
                {
                    object value = converter.ConvertFrom(setting);

                    // Set property
                    fastProp.SetValue(instance, value);
                }
                catch (Exception ex)
                {
                    var msg = "Could not convert setting '{0}' to type '{1}'".FormatInvariant(key, prop.PropertyType.Name);
                    Logger.Error(ex, msg);
                }
            }

            return instance;
        }

        #endregion

        #region ServiceScope

        protected override void OnClearCache()
        {
            _cache.Remove(SETTINGS_ALL_KEY);
        }

        protected internal static string CreateCacheKey(string name, int storeId)
        {
            return name.Trim().ToLowerInvariant() + "/" + storeId.ToString();
        }

        #endregion
    }
}
