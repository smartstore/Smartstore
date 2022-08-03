namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Localization service interface
    /// </summary>
    public partial interface ILocalizationService
    {
        /// <summary>
        /// Gets a resource string value for the given <paramref name="resourceKey"/>.
        /// </summary>
        /// <param name="resourceKey">A string representing a resource key.</param>
        /// <param name="languageId">Language identifier. Auto-resolves to working language id if <c>0</c>.</param>
        /// <param name="logIfNotFound">A value indicating whether to log a warning if locale string resource is not found.</param>
        /// <param name="defaultValue">Default value to return if resource is not found.</param>
        /// <param name="returnEmptyIfNotFound">A value indicating whether an empty string will be returned if a resource is not found and default value is set to empty string.</param>
        /// <returns>A string representing the requested resource string.</returns>
        string GetResource(string resourceKey,
            int languageId = 0,
            bool logIfNotFound = true,
            string defaultValue = "",
            bool returnEmptyIfNotFound = false);

        /// <summary>
        /// Gets a resource string value for the given <paramref name="resourceKey"/>.
        /// </summary>
        /// <param name="resourceKey">A string representing a resource key.</param>
        /// <param name="languageId">Language identifier. Auto-resolves to working language id if <c>0</c>.</param>
        /// <param name="logIfNotFound">A value indicating whether to log a warning if locale string resource is not found.</param>
        /// <param name="defaultValue">Default value to return if resource is not found.</param>
        /// <param name="returnEmptyIfNotFound">A value indicating whether an empty string will be returned if a resource is not found and default value is set to empty string.</param>
        /// <returns>A string representing the requested resource string.</returns>
        Task<string> GetResourceAsync(string resourceKey,
            int languageId = 0,
            bool logIfNotFound = true,
            string defaultValue = "",
            bool returnEmptyIfNotFound = false);

        /// <summary>
        /// Gets the localized value of an enum.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="enumValue">Enum value.</param>
        /// <param name="languageId">Language identifier. Auto-resolves to working language id if <c>0</c>.</param>
        /// <param name="hint">A value indicating whether to load the hint.</param>
        /// <returns>Localized value of an enum.</returns>
        string GetLocalizedEnum<T>(T enumValue, int languageId = 0, bool hint = false)
            where T : struct;

        /// <summary>
        /// Gets the localized value of an enum.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="enumValue">Enum value.</param>
        /// <param name="languageId">Language identifier. Auto-resolves to working language id if <c>0</c>.</param>
        /// <param name="hint">A value indicating whether to load the hint.</param>
        /// <returns>Localized value of an enum.</returns>
        Task<string> GetLocalizedEnumAsync<T>(T enumValue, int languageId = 0, bool hint = false)
            where T : struct;

        /// <summary>
        /// Gets a locale string resource from database.
        /// </summary>
        /// <param name="resourceName">A string representing a resource name.</param>
        /// <returns>A tracked locale string resource entity</returns>
        Task<LocaleStringResource> GetLocaleStringResourceByNameAsync(string resourceName);

        /// <summary>
        /// Gets a locale string resource from database.
        /// </summary>
        /// <param name="resourceName">A string representing a resource name.</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="logIfNotFound">A value indicating whether to log a warning if entity does not exist in database.</param>
        /// <returns>A tracked locale string resource entity</returns>
        Task<LocaleStringResource> GetLocaleStringResourceByNameAsync(string resourceName, int languageId, bool logIfNotFound = true);

        /// <summary>
        /// Deletes all string resource entities with names beginning with <paramref name="key"/>.
        /// This is a batch operation that does not invoke any database save hooks.
        /// </summary>
        /// <param name="key">e.g. SmartStore.SomePluginName</param>
        /// <returns>Number of deleted string resource entities.</returns>
        Task<int> DeleteLocaleStringResourcesAsync(string key, bool keyIsRootKey = true);
    }
}
