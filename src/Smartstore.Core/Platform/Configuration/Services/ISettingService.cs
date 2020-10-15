using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Smartstore.Core.Configuration
{
    public enum SaveSettingResult
    {
        Unchanged,
        Modified,
        Inserted,
        Deleted
    }

    [Serializable]
    public class CachedSetting
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int StoreId { get; set; }
    }

    /// <summary>
    /// Setting service interface
    /// </summary>
    public partial interface ISettingService : IScopedService
    {
        /// <summary>
        /// Determines whether a setting exists
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>true -setting exists; false - does not exist</returns>
        Task<bool> SettingExistsAsync<T, TPropType>(T settings, Expression<Func<T, TPropType>> keySelector, int storeId = 0)
            where T : ISettings, new();

        /// <summary>
        /// Get setting value by key
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="loadSharedValueIfNotFound">A value indicating whether a shared (for all stores) value should be loaded if a value specific for a certain store is not found</param>
        /// <returns>Setting value</returns>
        Task<T> GetSettingByKeyAsync<T>(string key, T defaultValue = default, int storeId = 0, bool loadSharedValueIfNotFound = false);

        /// <summary>
        /// Gets a setting by key
        /// </summary>
        /// <param name="key">Unique setting key</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Setting</returns>
        Task<Setting> GetSettingEntityByKeyAsync(string key, int storeId = 0);

        /// <summary>
        /// Load settings
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier for which settigns should be loaded</param>
        T LoadSettings<T>(int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Load settings
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier for which settigns should be loaded</param>
        Task<T> LoadSettingsAsync<T>(int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Load settings
        /// </summary>
        /// <param name="settingType">Setting class type</param>
        /// <param name="storeId">Store identifier for which settigns should be loaded</param>
        ISettings LoadSettings(Type settingType, int storeId = 0);

        /// <summary>
        /// Load settings
        /// </summary>
        /// <param name="settingType">Setting class type</param>
        /// <param name="storeId">Store identifier for which settigns should be loaded</param>
        Task<ISettings> LoadSettingsAsync(Type settingType, int storeId = 0);

        /// <summary>
        /// Set setting value
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier</param>
        Task<SaveSettingResult> SetSettingAsync<T>(string key, T value, int storeId = 0);

        /// <summary>
        /// Save settings object
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="settings">Setting instance</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns><c>true</c> when any setting property has been modified.</returns>
        Task<bool> SaveSettingsAsync<T>(T settings, int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Save settings object
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        /// <param name="clearCache">A value indicating whether to clear cache after setting update</param>
        /// <returns><c>true</c> when the setting property has been modified.</returns>
        Task<SaveSettingResult> SaveSettingAsync<T, TPropType>(
            T settings, 
            Expression<Func<T, TPropType>> keySelector, 
            int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Updates a setting property
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        /// <returns><c>true</c> when the setting property has been modified.</returns>
        Task<SaveSettingResult> UpdateSettingAsync<T, TPropType>(
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            bool overrideForStore,
            int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Delete all settings
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        Task<int> DeleteSettingsAsync<T>() where T : ISettings, new();

        /// <summary>
        /// Delete a settings property from storage
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        /// <returns><c>true</c> when the setting existed and has been deleted</returns>
        Task<bool> DeleteSettingAsync<T, TPropType>(
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Delete a settings property from storage
        /// </summary>
        /// <returns><c>true</c> when the setting existed and has been deleted</returns>
        Task<bool> DeleteSettingAsync(string key, int storeId = 0);

        /// <summary>
        /// Deletes all settings with its key beginning with rootKey.
        /// </summary>
        /// <returns>Number of deleted settings</returns>
        Task<int> DeleteSettingsAsync(string rootKey);
    }
}
