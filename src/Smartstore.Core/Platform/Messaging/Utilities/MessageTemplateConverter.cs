using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Engine;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Messaging.Utilities
{
    public sealed class MessageTemplateConverter
    {
        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly EmailAccount _defaultEmailAccount;

        public MessageTemplateConverter(SmartDbContext db, IApplicationContext appContext)
        {
            _db = Guard.NotNull(db, nameof(db));
            _appContext = Guard.NotNull(appContext, nameof(appContext));
            _defaultEmailAccount = _db.Set<EmailAccount>().FirstOrDefault(x => x.Email != null);
        }

        /// <summary>
        /// Loads a single message template from file and deserializes its XML content.
        /// </summary>
        /// <param name="templateName">Name of template without extension, e.g. 'GiftCard.Notification'</param>
        /// <param name="culture">Language ISO code</param>
        /// <param name="rootPath">The application root path of template to load, e.g. "/Modules/MyModule/App_Data/EmailTemplates". Default is "/App_Data/EmailTemplates".</param>
        /// <returns>Deserialized template xml</returns>
        public MessageTemplate Load(string templateName, string culture, string rootPath = null)
        {
            Guard.NotEmpty(templateName, nameof(templateName));
            Guard.NotEmpty(culture, nameof(culture));

            var root = _appContext.ContentRoot;
            var dir = ResolveTemplateDirectory(culture, rootPath);
            var file = root.GetFile(root.PathCombine(dir.SubPath, templateName + ".xml"));

            if (!file.Exists)
            {
                throw new FileNotFoundException($"File '{file.SubPath}' does not exist.");
            }

            return DeserializeTemplate(file);
        }

        /// <summary>
        /// Loads all message templates from disk
        /// </summary>
        /// <param name="culture">Language ISO code</param>
        /// <param name="rootPath">The application root path of templates to load, e.g. "/Modules/MyModule/App_Data/EmailTemplates". Default is "/App_Data/EmailTemplates".</param>
        /// <returns>List of deserialized template xml</returns>
        public IEnumerable<MessageTemplate> LoadAll(string culture, string rootPath = null)
        {
            Guard.NotEmpty(culture, nameof(culture));

            var dir = ResolveTemplateDirectory(culture, rootPath);
            var files = dir.EnumerateFiles("*.xml");

            foreach (var file in files)
            {
                var template = DeserializeTemplate(file);
                template.Name = file.NameWithoutExtension;
                yield return template;
            }
        }

        public MessageTemplate Deserialize(string xml, string templateName)
        {
            Guard.NotEmpty(xml, nameof(xml));
            Guard.NotEmpty(templateName, nameof(templateName));

            var template = DeserializeDocument(XDocument.Parse(xml));
            template.Name = templateName;
            return template;
        }

        public XmlDocument Save(MessageTemplate template, string culture)
        {
            Guard.NotNull(template, nameof(template));
            Guard.NotEmpty(culture, nameof(culture));

            var doc = new XmlDocument();
            doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><MessageTemplate></MessageTemplate>");

            var docRoot = doc.DocumentElement;
            docRoot.AppendChild(doc.CreateElement("To")).InnerText = template.To;
            if (template.ReplyTo.HasValue())
                docRoot.AppendChild(doc.CreateElement("ReplyTo")).InnerText = template.ReplyTo;
            docRoot.AppendChild(doc.CreateElement("Subject")).InnerText = template.Subject;
            docRoot.AppendChild(doc.CreateElement("ModelTypes")).InnerText = template.ModelTypes;
            docRoot.AppendChild(doc.CreateElement("Body")).AppendChild(doc.CreateCDataSection(template.Body));

            var root = _appContext.ContentRoot;
            var dir = root.GetDirectory(root.PathCombine("/App_Data/EmailTemplates", culture));
            dir.Create();

            // File path
            var filePath = root.PathCombine(dir.SubPath, template.Name + ".xml");

            var xml = Prettifier.PrettifyXML(doc.OuterXml);
            root.WriteAllText(filePath, xml);

            return doc;
        }

        /// <summary>
        /// Imports all template xml files to <see cref="MessageTemplate"/> table.
        /// </summary>
        /// <param name="rootPath">The application root path of templates to import, e.g. "/Modules/MyModule/App_Data/EmailTemplates". Default is "/App_Data/EmailTemplates".</param>
        public async Task ImportAllAsync(string culture, string rootPath = null)
        {
            var sourceTemplates = LoadAll(culture, rootPath);
            var dbTemplatesMap = (await _db.MessageTemplates
                .ToListAsync())
                .ToMultimap(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var source in sourceTemplates)
            {
                if (dbTemplatesMap.ContainsKey(source.Name))
                {
                    foreach (var target in dbTemplatesMap[source.Name])
                    {
                        if (source.To.HasValue()) target.To = source.To;
                        if (source.ReplyTo.HasValue()) target.ReplyTo = source.ReplyTo;
                        if (source.Subject.HasValue()) target.Subject = source.Subject;
                        if (source.ModelTypes.HasValue()) target.ModelTypes = source.ModelTypes;
                        if (source.Body.HasValue()) target.Body = source.Body;
                    }
                }
                else
                {
                    var template = new MessageTemplate
                    {
                        Name = source.Name,
                        To = source.To,
                        ReplyTo = source.ReplyTo,
                        Subject = source.Subject,
                        ModelTypes = source.ModelTypes,
                        Body = source.Body,
                        IsActive = true,
                        EmailAccountId = (_defaultEmailAccount?.Id).GetValueOrDefault(),
                    };

                    _db.MessageTemplates.Add(template);
                }
            }

            await _db.SaveChangesAsync();
        }

        private IDirectory ResolveTemplateDirectory(string culture, string rootPath = null)
        {
            var root = _appContext.ContentRoot;
            var dir = root.GetDirectory(rootPath.NullEmpty() ?? "/App_Data/EmailTemplates/");

            // de-DE, de, en
            var testPaths = new List<string>(3) { culture, "en" };
            if (culture.IndexOf('-') > -1)
            {
                testPaths.Insert(1, culture.Substring(0, 2));
            }

            foreach (var path in testPaths.Select(x => PathUtility.Combine(dir.SubPath, x)))
            {
                var subDir = root.GetDirectory(path);
                if (subDir.Exists)
                {
                    return subDir;
                }
            }

            throw new DirectoryNotFoundException($"Could not obtain an email templates path for language {culture}. Fallback to 'en' failed, because directory does not exist.");
        }

        private static MessageTemplate DeserializeTemplate(IFile file)
        {
            using var stream = file.OpenRead();
            return DeserializeDocument(XDocument.Load(stream));
        }

        private static MessageTemplate DeserializeDocument(XDocument doc)
        {
            var root = doc.Root;
            var result = new MessageTemplate();

            foreach (var node in root.Nodes().OfType<XElement>())
            {
                var value = node.Value.Trim();

                switch (node.Name.LocalName)
                {
                    case "To":
                        result.To = value;
                        break;
                    case "ReplyTo":
                        result.ReplyTo = value;
                        break;
                    case "Subject":
                        result.Subject = value;
                        break;
                    case "ModelTypes":
                        result.ModelTypes = value;
                        break;
                    case "Body":
                        result.Body = value;
                        break;
                }
            }

            return result;
        }
    }
}
