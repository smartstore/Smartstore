using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Smartstore.Core.DataExchange;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Localization service interface
    /// </summary>
    public interface ILocalizationService
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

        /// <summary>
        /// Export language resources to xml
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>Result as XML string</returns>
        Task<string> ExportResourcesToXmlAsync(Language language);

        /// <summary>
        /// Imports language resources from XML file.
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="xmlDocument">XML document</param>
        /// <param name="rootKey">Prefix for resource key name</param>
        /// <param name="mode">Specifies whether resources should be inserted or updated (or both)</param>
        /// <param name="updateTouchedResources">Specifies whether user touched resources should also be updated</param>
        /// <returns>The number of processed (added or updated) resource entries</returns>
        Task<int> ImportResourcesFromXmlAsync(
            Language language,
            XmlDocument xmlDocument,
            string rootKey = null,
            bool sourceIsPlugin = false,
            ImportModeFlags mode = ImportModeFlags.Insert | ImportModeFlags.Update,
            bool updateTouchedResources = false);

        /// <summary>
        /// Imports module resources from xml files in module's localization directory. 
        /// Note: Deletes existing resources before importing.
        /// </summary>
        /// <param name="moduleDescriptor">Descriptor of the module</param>
        /// <param name="targetList">Load them into the passed list rather than database</param>
        /// <param name="updateTouchedResources">Specifies whether user touched resources should also be updated</param>	
        /// <param name="filterLanguages">Import only files for particular languages</param>
        Task ImportModuleResourcesFromXmlAsync(
            ModuleDescriptor moduleDescriptor,
            IList<LocaleStringResource> targetList = null,
            bool updateTouchedResources = true,
            List<Language> filterLanguages = null);

        /// <summary>
        /// Flattens all nested <c>LocaleResource</c> child nodes into a new document.
        /// </summary>
        /// <param name="source">The source xml resource file</param>
        /// <returns>
        /// Either a new document with flattened resources or - if no nesting is determined - 
        /// the original document, which was passed as <paramref name="source"/>.
        /// </returns>
        XmlDocument FlattenResourceFile(XmlDocument source);

        /// <summary>
        /// Creates a directory hasher used to determine module localization changes across app startups.
        /// </summary>
        /// <param name="moduleDescriptor">Descriptor of the module</param>
        /// <returns>The hasher impl</returns>
        DirectoryHasher CreateModuleResourcesHasher(ModuleDescriptor moduleDescriptor);
    }
}
