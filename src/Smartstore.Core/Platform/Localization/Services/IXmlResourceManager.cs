using System.Xml;
using Smartstore.Core.DataExchange;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Responsible for importing and exporting locale string resources from and to XML.
    /// </summary>
    public partial interface IXmlResourceManager
    {
        /// <summary>
        /// Export language resources to xml
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>Result as XML string</returns>
        Task<string> ExportResourcesToXmlAsync(Language language);

        /// <summary>
        /// Imports language resources from XML file. This method commits to db.
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
        /// This method commits to db.
        /// </summary>
        /// <param name="moduleDescriptor">Descriptor of the module</param>
        /// <param name="targetList">Load them into the passed list rather than database</param>
        /// <param name="updateTouchedResources">Specifies whether user touched resources should also be updated</param>	
        /// <param name="filterLanguages">Import only files for particular languages</param>
        Task ImportModuleResourcesFromXmlAsync(
            IModuleDescriptor moduleDescriptor,
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
        /// <returns>The hasher impl or <c>null</c> if the localization directory does not exist.</returns>
        DirectoryHasher CreateModuleResourcesHasher(IModuleDescriptor moduleDescriptor);
    }
}
