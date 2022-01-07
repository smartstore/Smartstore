using Smartstore.Core.Configuration;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Localized entity service interface
    /// </summary>
    public partial interface ILocalizedEntityService
    {
        /// <summary>
        /// Finds a localized value.
        /// </summary>
        /// <param name="languageId">Language identifier</param>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="localeKeyGroup">Locale key group</param>
        /// <param name="localeKey">Locale key</param>
        /// <returns>Found localized value</returns>
        string GetLocalizedValue(int languageId, int entityId, string localeKeyGroup, string localeKey);

        /// <summary>
        /// Finds a localized value.
        /// </summary>
        /// <param name="languageId">Language identifier</param>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="localeKeyGroup">Locale key group</param>
        /// <param name="localeKey">Locale key</param>
        /// <returns>Found localized value</returns>
        Task<string> GetLocalizedValueAsync(int languageId, int entityId, string localeKeyGroup, string localeKey);

        /// <summary>
        /// Prefetches a collection of localized properties for a range of entities in one go
        /// and caches them for the duration of the current request.
        /// </summary>
        /// <param name="localeKeyGroup">Locale key group (scope)</param>
        /// <param name="entityIds">
        /// The entity ids to prefetch translations for. Can be null,
        /// in which case all translations for the requested scope are loaded.
        /// </param>
        /// <param name="isRange">Whether <paramref name="entityIds"/> represents a range of ids (perf).</param>
        /// <param name="isSorted">Whether <paramref name="entityIds"/> is already sorted (perf).</param>
        /// <remarks>
        /// Be careful not to load large amounts of data at once (e.g. for "Product" scope with large range).
        /// </remarks>
        Task PrefetchLocalizedPropertiesAsync(string localeKeyGroup, int languageId, int[] entityIds, bool isRange = false, bool isSorted = false);

        /// <summary>
        /// Gets a collection of localized properties for a range of entities in one go.
        /// </summary>
        /// <param name="localeKeyGroup">Locale key group (scope)</param>
        /// <param name="entityIds">
        /// The entity ids to load translations for. Can be null,
        /// in which case all translations for the requested scope are loaded.
        /// </param>
        /// <param name="isRange">Whether <paramref name="entityIds"/> represents a range of ids (perf).</param>
        /// <param name="isSorted">Whether <paramref name="entityIds"/> is already sorted (perf).</param>
        /// <returns>Localized property collection</returns>
        /// <remarks>
        /// Be careful not to load large amounts of data at once (e.g. for "Product" scope with large range).
        /// </remarks>
        Task<LocalizedPropertyCollection> GetLocalizedPropertyCollectionAsync(string localeKeyGroup, int[] entityIds, bool isRange = false, bool isSorted = false);

        /// <summary>
        /// Applies a localized property value. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="value">Locale value</param>
        /// <param name="languageId">Language ID</param>
        Task ApplyLocalizedValueAsync<T>(
            T entity,
            Expression<Func<T, string>> keySelector,
            string value,
            int languageId) where T : class, ILocalizedEntity;

        /// <summary>
        /// Applies a localized property value. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="value">Locale value</param>
        /// <param name="languageId">Language ID</param>
        Task ApplyLocalizedValueAsync<T, TPropType>(
            T entity,
            Expression<Func<T, TPropType>> keySelector,
            TPropType value,
            int languageId) where T : class, ILocalizedEntity;

        /// <summary>
        /// Applies a localized setting property value. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="TSetting">Setting impl type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings instance</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="value">Locale value</param>
        /// <param name="languageId">Language ID</param>
        /// <param name="storeId">Store ID</param>
        Task ApplyLocalizedSettingAsync<TSetting, TPropType>(
            TSetting settings,
            Expression<Func<TSetting, TPropType>> keySelector,
            TPropType value,
            int languageId,
            int storeId = 0) where TSetting : class, ISettings;

        /// <summary>
        /// Clears the value cache completely.
        /// </summary>
        Task ClearCacheAsync();
    }
}
