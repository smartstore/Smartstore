namespace Smartstore.Core.Configuration
{
    public enum ApplySettingResult
    {
        Unchanged,
        Modified,
        Inserted,
        Deleted
    }

    /// <summary>
    /// Reads and writes setting entities.
    /// None of the write methods should actually commit changes to database. It's the callers'
    /// reponsibility to call 'DbContext.SaveChanges()'.
    /// </summary>
    public partial interface ISettingService
    {
        /// <summary>
        /// Checks whether a setting for the given store exists.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns><c>true</c> if setting exists in database.</returns>
        Task<bool> SettingExistsAsync(string key, int storeId = 0);

        /// <summary>
        /// Get setting value by key
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="doFallback">A value indicating whether a shared (for all stores) value should be loaded if a value specific for a certain store is not found</param>
        /// <returns>Setting value</returns>
        T GetSettingByKey<T>(string key, T defaultValue = default, int storeId = 0, bool doFallback = false);

        /// <summary>
        /// Get setting value by key
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="doFallback">A value indicating whether a shared (for all stores) value should be loaded if a value specific for a certain store is not found</param>
        /// <returns>Setting value</returns>
        Task<T> GetSettingByKeyAsync<T>(string key, T defaultValue = default, int storeId = 0, bool doFallback = false);

        /// <summary>
        /// Gets a setting by key
        /// </summary>
        /// <param name="key">Unique setting key</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Setting</returns>
        Task<Setting> GetSettingEntityByKeyAsync(string key, int storeId = 0);

        /// <summary>
        /// Applies a setting value. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param>
        Task<ApplySettingResult> ApplySettingAsync<T>(string key, T value, int storeId = 0);

        /// <summary>
        /// Removes all settings with its key beginning with rootKey. The caller is responsible for database commit.
        /// </summary>
        /// <returns>Number of deleted settings</returns>
        Task<int> RemoveSettingsAsync(string rootKey);

        /// <summary>
        /// Removes a setting. The caller is responsible for database commit.
        /// </summary>
        /// <returns><c>true</c> when the setting exists in the database.</returns>
        Task<bool> RemoveSettingAsync(string key, int storeId = 0);
    }
}
