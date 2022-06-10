namespace Smartstore.Core.Configuration
{
    /// <summary>
    /// Responsible for activating and populating setting class instances that implement <see cref="ISettings"/>.
    /// Instances are cached as singleton objects: CacheKey is composed of class name and StoreId.
    /// </summary>
    public interface ISettingFactory
    {
        /// <summary>
        /// Loads settings.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier for which settings should be loaded</param>
        T LoadSettings<T>(int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Loads settings.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store identifier for which settings should be loaded</param>
        Task<T> LoadSettingsAsync<T>(int storeId = 0) where T : ISettings, new();

        /// <summary>
        /// Loads settings.
        /// </summary>
        /// <param name="settingType">Setting class type</param>
        /// <param name="storeId">Store identifier for which settings should be loaded</param>
        ISettings LoadSettings(Type settingType, int storeId = 0);

        /// <summary>
        /// Loads settings.
        /// </summary>
        /// <param name="settingType">Setting class type</param>
        /// <param name="storeId">Store identifier for which settings should be loaded</param>
        Task<ISettings> LoadSettingsAsync(Type settingType, int storeId = 0);

        /// <summary>
        /// Save settings object. This methods commits changes to database.
        /// </summary>
        /// <typeparam name="T">Settings type</typeparam>
        /// <param name="settings">Setting instance</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>The number of setting entities committed to database.</returns>
        Task<int> SaveSettingsAsync<T>(T settings, int storeId = 0) where T : ISettings, new();
    }
}