using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Utilities;

namespace Smartstore.Core.Configuration;

public class SettingFactory : ISettingFactory
{
    private readonly IComponentContext _scope;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICacheManager _cache;
    private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;

    private static readonly ConcurrentDictionary<Type, Func<ISettings>> _activators = new();
    private static readonly ConcurrentDictionary<Type, (FastProperty FastProp, string Key)[]> _loadProps = new();
    private static readonly ConcurrentDictionary<Type, (FastProperty FastProp, string Key)[]> _saveProps = new();

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
        Guard.NotNull(settingsType);
        Guard.HasDefaultConstructor(settingsType);

        if (!typeof(ISettings).IsAssignableFrom(settingsType))
        {
            throw new ArgumentException($"The type to load settings for must be a subclass of the '{typeof(ISettings).FullName}' interface", nameof(settingsType));
        }

        var cacheKey = SettingService.BuildCacheKeyForClassAccess(settingsType, storeId);

        return _cache.Get(cacheKey, o =>
        {
            o.ExpiresIn(SettingService.DefaultExpiry);

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
        Guard.NotNull(settingsType);
        Guard.HasDefaultConstructor(settingsType);

        if (!typeof(ISettings).IsAssignableFrom(settingsType))
        {
            throw new ArgumentException($"The type to load settings for must be a subclass of the '{typeof(ISettings).FullName}' interface", nameof(settingsType));
        }

        var cacheKey = SettingService.BuildCacheKeyForClassAccess(settingsType, storeId);

        return _cache.GetAsync(cacheKey, async (o) =>
        {
            o.ExpiresIn(SettingService.DefaultExpiry);

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
        Guard.NotNull(db);
        Guard.NotNull(settings);

        var settingsType = settings.GetType();
        var hasChanges = false;

        var rawSettings = await GetRawSettingsAsync(db, settingsType, storeId, doFallback: false, tracked: true);

        foreach (var (fastProp, key) in _saveProps.GetOrAdd(settingsType, BuildSaveProps))
        {
            var currentValue = fastProp.GetValue(settings).Convert<string>();

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
                db.Settings.Add(new Setting
                {
                    Name = key,
                    Value = currentValue,
                    StoreId = storeId
                });
                hasChanges = true;
            }
        }

        return hasChanges ? await db.SaveChangesAsync() : 0;
    }

    #region Utils

    private IDisposable GetOrCreateDbContext(out SmartDbContext db)
    {
        db = _scope?.ResolveOptional<SmartDbContext>() ??
             _httpContextAccessor.HttpContext?.RequestServices?.GetService<SmartDbContext>();

        if (db != null)
        {
            // Don't dispose request scoped main db instance.
            return ActionDisposable.Empty;
        }

        // Fetch a fresh DbContext if no scope is given.
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
        Guard.NotNull(settingsType);
        Guard.NotNull(rawSettings);

        var instance = (ISettings)Activator.CreateInstance(settingsType);
        var prefix = settingsType.Name;

        foreach (var (fastProp, key) in _loadProps.GetOrAdd(settingsType, BuildLoadProps))
        {
            rawSettings.TryGetValue(key, out var rawSetting);
            var valueStr = rawSetting?.Value;

            if (valueStr == null)
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

            try
            {
                fastProp.SetValue(instance, TypeConverterFactory.GetConverter(fastProp.Property.PropertyType).ConvertFrom(valueStr));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not convert setting '{0}' to type '{1}'".FormatInvariant(key, fastProp.Property.PropertyType.Name));
            }
        }

        return instance;
    }

    private static (FastProperty FastProp, string Key)[] BuildLoadProps(Type settingsType)
    {
        var prefix = settingsType.Name;
        var result = new List<(FastProperty, string)>();

        foreach (var fastProp in FastProperty.GetProperties(settingsType).Values)
        {
            if (!fastProp.Property.CanWrite)
                continue;

            if (!TypeConverterFactory.GetConverter(fastProp.Property.PropertyType).CanConvertFrom(typeof(string)))
                continue;

            result.Add((fastProp, prefix + '.' + fastProp.Name));
        }

        return [.. result];
    }

    private static (FastProperty FastProp, string Key)[] BuildSaveProps(Type settingsType)
    {
        var prefix = settingsType.Name;
        var result = new List<(FastProperty, string)>();

        foreach (var fastProp in FastProperty.GetProperties(settingsType).Values)
        {
            if (!fastProp.IsPublicSettable)
                continue;

            if (!TypeConverterFactory.GetConverter(fastProp.Property.PropertyType).CanConvertFrom(typeof(string)))
                continue;

            result.Add((fastProp, prefix + '.' + fastProp.Name));
        }

        return [.. result];
    }

    #endregion
}