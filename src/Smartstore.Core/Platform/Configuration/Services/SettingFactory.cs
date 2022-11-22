using System.Data;
using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Utilities;

namespace Smartstore.Core.Configuration
{
    public class SettingFactory : ISettingFactory
    {
        private readonly IComponentContext _scope;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheManager _cache;
        private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;

        public SettingFactory(
            IHttpContextAccessor httpContextAccessor,
            ICacheManager cache,
            IDbContextFactory<SmartDbContext> dbContextFactory)
            : this(null, httpContextAccessor, cache, dbContextFactory)
        {
        }

        internal SettingFactory(
            IComponentContext scope,
            IHttpContextAccessor httpContextAccessor,
            ICacheManager cache,
            IDbContextFactory<SmartDbContext> dbContextFactory)
        {
            _scope = scope;
            _cache = cache;
            _dbContextFactory = dbContextFactory;
            _httpContextAccessor = httpContextAccessor;
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

                using (GetOrCreateDbContext(out var db))
                {
                    
                    var rawSettings = GetRawSettings(db, settingsType, storeId, true, false);
                    return MaterializeSettings(settingsType, rawSettings);
                }
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

                using (GetOrCreateDbContext(out var db))
                {
                    var rawSettings = await GetRawSettingsAsync(db, settingsType, storeId, true, false);
                    return MaterializeSettings(settingsType, rawSettings);
                }
            }, independent: true, allowRecursion: true);
        }

        /// <inheritdoc/>
        public async Task<int> SaveSettingsAsync<T>(T settings, int storeId = 0) where T : ISettings, new()
        {
            // INFO: Let SettingService's hook handler handle cache invalidation
            using (GetOrCreateDbContext(out var db))
            {
                var numSaved = await SaveSettingsAsync(db, settings, true, storeId);
                if (numSaved > 0)
                {
                    // Prevent reloading from DB on next hit
                    await _cache.PutAsync(
                        SettingService.BuildCacheKeyForClassAccess(typeof(T), storeId),
                        settings,
                        new CacheEntryOptions().ExpiresIn(SettingService.DefaultExpiry));
                }

                return numSaved;
            }
        }

        /// <summary>
        /// Internal API.
        /// </summary>
        public static async Task<int> SaveSettingsAsync(SmartDbContext db, ISettings settings, bool overwriteExisting = true, int storeId = 0)
        {
            Guard.NotNull(db, nameof(db));
            Guard.NotNull(settings, nameof(settings));

            var settingsType = settings.GetType();
            var prefix = settingsType.Name;
            var hasChanges = false;

            var rawSettings = await GetRawSettingsAsync(db, settingsType, storeId, doFallback: false, tracked: true);

            foreach (var prop in FastProperty.GetProperties(settingsType).Values)
            {
                // Get only properties we can read and write to
                if (!prop.IsPublicSettable)
                    continue;

                var converter = TypeConverterFactory.GetConverter(prop.Property.PropertyType);
                if (converter == null || !converter.CanConvertFrom(typeof(string)))
                    continue;

                var key = prefix + '.' + prop.Name;
                var currentValue = prop.GetValue(settings).Convert<string>();

                if (rawSettings.TryGetValue(key, out var setting))
                {
                    if ((overwriteExisting || setting.Value == null) && setting.Value != currentValue && currentValue != null)
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
            return numSaved;
        }

        #region Utils

        private IDisposable GetOrCreateDbContext(out SmartDbContext db)
        {
            db = _scope?.ResolveOptional<SmartDbContext>() ??
                 _httpContextAccessor.HttpContext?.RequestServices?.GetService<SmartDbContext>();

            if (db != null)
            {
                var conState = db.Database.GetDbConnection().State;
                if (conState == ConnectionState.Closed)
                {
                    // Don't dispose request scoped main db instance.
                    return ActionDisposable.Empty;
                }
            }

            // Fetch a fresh DbContext if no scope is given or current connection is not in "Closed" state.
            db = _dbContextFactory.CreateDbContext();

            return db;
        }

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
