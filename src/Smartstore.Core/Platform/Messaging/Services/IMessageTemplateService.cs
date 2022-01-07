using System.Xml;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Provides message template utilities
    /// </summary>
    public interface IMessageTemplateService
    {
        /// <summary>
        /// Creates a copy of a message template with all dependant data and saves the copy into the database.
        /// </summary>
        /// <param name="source">The source template to copy.</param>
        /// <returns>Message template copy</returns>
        Task<MessageTemplate> CopyTemplateAsync(MessageTemplate source);

        /// <summary>
        /// Loads a single message template from file and deserializes its XML content.
        /// </summary>
        /// <param name="templateName">Name of template without extension, e.g. 'GiftCard.Notification'</param>
        /// <param name="culture">Language ISO code</param>
        /// <param name="rootPath">The application root path of template to load, e.g. "/Modules/MyModule/App_Data/EmailTemplates". Default is "/App_Data/EmailTemplates".</param>
        /// <returns>Deserialized template xml</returns>
        MessageTemplate LoadTemplate(string templateName, string culture, string rootPath = null);

        /// <summary>
        /// Deserializes a message templates from XML.
        /// </summary>
        /// <param name="xml">Source XML</param>
        /// <param name="templateName">Name of template</param>
        MessageTemplate DeserializeTemplate(string xml, string templateName);

        /// <summary>
        /// Serializes and saves a message template entity to disk.
        /// </summary>
        /// <param name="template">Source template entity</param>
        /// <param name="culture">Language ISO code. Appended to output directory path.</param>
        /// <returns>The result XML document.</returns>
        XmlDocument SaveTemplate(MessageTemplate template, string culture);

        /// <summary>
        /// Imports all template xml files to <see cref="MessageTemplate"/> table.
        /// </summary>
        /// <param name="rootPath">The application root path of templates to import, e.g. "/Modules/MyModule/App_Data/EmailTemplates". Default is "/App_Data/EmailTemplates".</param>
        Task ImportAllTemplatesAsync(string culture, string rootPath = null);
    }
}
