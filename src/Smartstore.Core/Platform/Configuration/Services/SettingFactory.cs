using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;

namespace Smartstore.Core.Configuration
{
    public class SettingFactory : ISettingFactory
    {
        private readonly ICacheManager _cache;
        private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;

        public SettingFactory(ICacheManager cache, IDbContextFactory<SmartDbContext> dbContextFactory)
        {
            _cache = cache;
            _dbContextFactory = dbContextFactory;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LoadSettings<T>(int storeId = 0) where T : ISettings, new()
        {
            return (T)LoadSettings(typeof(T), storeId);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> LoadSettingsAsync<T>(int storeId = 0) where T : ISettings, new()
        {
            return (T)(await LoadSettingsAsync(typeof(T), storeId));
        }

        /// <inheritdoc/>
        public ISettings LoadSettings(Type settingsType, int storeId = 0)
        {
            Guard.NotNull(settingsType, nameof(settingsType));
            Guard.HasDefaultConstructor(settingsType);

            if (!typeof(ISettings).IsAssignableFrom(settingsType))
            {
                throw new ArgumentException($"The type to load settings for must be a subclass of the '{typeof(ISettings).FullName}' interface", nameof(settingsType));
            }

            var cacheKey = SettingService.BuildCacheKeyForClassAccess(settingsType, storeId);

            return _cache.Get(cacheKey, o =>
            {
                o.ExpiresIn(TimeSpan.FromHours(8));

                using var db = _dbContextFactory.CreateDbContext();
                var rawSettings = GetRawSettings(db, settingsType, storeId, true, false);
                return MaterializeSettings(settingsType, rawSettings);
            }, independent: true, allowRecursion: true);
        }

        /// <inheritdoc/>
        public Task<ISettings> LoadSettingsAsync(Type settingsType, int storeId = 0)
        {
            Guard.NotNull(settingsType, nameof(settingsType));
            Guard.HasDefaultConstructor(settingsType);

            if (!typeof(ISettings).IsAssignableFrom(settingsType))
            {
                throw new ArgumentException($"The type to load settings for must be a subclass of the '{typeof(ISettings).FullName}' interface", nameof(settingsType));
            }

            var cacheKey = SettingService.BuildCacheKeyForClassAccess(settingsType, storeId);

            return _cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(8));

                using var db = _dbContextFactory.CreateDbContext();
                var rawSettings = await GetRawSettingsAsync(db, settingsType, storeId, true, false);
                return MaterializeSettings(settingsType, rawSettings);
            }, independent: true, allowRecursion: true);
        }

        /// <inheritdoc/>
        public async Task<int> SaveSettingsAsync<T>(T settings, int storeId = 0) where T : ISettings, new()
        {
            Guard.NotNull(settings, nameof(settings));

            // INFO: Let SettingService's hook handler handle cache invalidation

            var settingsType = typeof(T);
            var prefix = settingsType.Name;
            var hasChanges = false;

            using var db = _dbContextFactory.CreateDbContext();
            var rawSettings = await GetRawSettingsAsync(db, settingsType, storeId, false, true);

            foreach (var prop in FastProperty.GetProperties(settingsType).Values)
            {
                // Get only properties we can read and write to
                if (!prop.IsPublicSettable)
                    continue;

                var converter = TypeConverterFactory.GetConverter(prop.Property.PropertyType);
                if (converter == null || !converter.CanConvertFrom(typeof(string)))
                    continue;

                string key = prefix + "." + prop.Name;
                string currentValue = prop.GetValue(settings).Convert<string>();

                if (rawSettings.TryGetValue(key, out var setting))
                {
                    if (setting.Value != currentValue)
                    {
                        // Update
                        setting.Value = currentValue;
                        hasChanges = true;
                    }
                }
                else
                {
                    // Insert
                    setting = new Setting
                    {
                        Name = key.ToLowerInvariant(),
                        Value = currentValue,
                        StoreId = storeId
                    };

                    hasChanges = true;
                    db.Settings.Add(setting);
                }
            }

            var numSaved = hasChanges ? await db.SaveChangesAsync() : 0;

            if (numSaved > 0)
            {
                // Prevent reloading from DB on next hit
                await _cache.PutAsync(
                    SettingService.BuildCacheKeyForClassAccess(settingsType, storeId), 
                    settings, 
                    new CacheEntryOptions().ExpiresIn(SettingService.DefaultExpiry));
            }

            return numSaved;
        }

        #region Utils

        private static IDictionary<string, Setting> GetRawSettings(SmartDbContext db, Type settingsType, int storeId, bool doFallback, bool tracked = false)
        {
            var list = db.Settings
                .ApplyTracking(tracked)
                .ApplyClassFilter(settingsType, storeId, doFallback)
                .ApplySorting()
                .ToList();

            // Because the list is sorted by StoreId, store-specific entries overwrite neutral ones.
            return list.ToDictionarySafe(k => k.Name, e => e, StringComparer.OrdinalIgnoreCase);
        }

        private static async Task<IDictionary<string, Setting>> GetRawSettingsAsync(SmartDbContext db, Type settingsType, int storeId, bool doFallback, bool tracked = false)
        {
            var list = await db.Settings
                .ApplyTracking(tracked)
                .ApplyClassFilter(settingsType, storeId, doFallback)
                .ApplySorting()
                .ToListAsync();

            // Because the list is sorted by StoreId, store-specific entries overwrite neutral ones.
            return list.ToDictionarySafe(k => k.Name, e => e, StringComparer.OrdinalIgnoreCase);
        }

        private ISettings MaterializeSettings(Type settingsType, IDictionary<string, Setting> rawSettings)
        {
            Guard.NotNull(settingsType, nameof(settingsType));
            Guard.NotNull(rawSettings, nameof(rawSettings));

            var instance = (ISettings)Activator.CreateInstance(settingsType);
            var prefix = settingsType.Name;

            foreach (var fastProp in FastProperty.GetProperties(settingsType).Values)
            {
                var prop = fastProp.Property;

                // Get properties we can read and write to
                if (!prop.CanWrite)
                    continue;

                string key = prefix + "." + prop.Name;
                rawSettings.TryGetValue(key, out var rawSetting);

                string setting = rawSetting?.Value;

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
    }
}
