using System.Xml;
using Smartstore.Core.DataExchange;

namespace Smartstore.Core.Localization
{
    public static partial class IXmlResourceManagerExtensions
    {
        /// <summary>
        /// Imports language resources from XML file. This method commits to db.
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="xml">XML document</param>
        /// <param name="rootKey">Prefix for resource key name</param>
        /// <param name="mode">Specifies whether resources should be inserted or updated (or both)</param>
        /// <param name="updateTouchedResources">Specifies whether user touched resources should also be updated</param>
        /// <returns>The number of processed (added or updated) resource entries</returns>
        public static Task<int> ImportResourcesFromXmlAsync(this IXmlResourceManager manager,
            Language language,
            string xml,
            string rootKey = null,
            bool sourceIsPlugin = false,
            ImportModeFlags mode = ImportModeFlags.Insert | ImportModeFlags.Update,
            bool updateTouchedResources = false)
        {
            if (string.IsNullOrEmpty(xml))
                return Task.FromResult(0);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            return manager.ImportResourcesFromXmlAsync(language, xmlDoc, rootKey, sourceIsPlugin, mode, updateTouchedResources);
        }
    }
}
