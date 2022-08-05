using System.Xml;
using System.Xml.Linq;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Messaging
{
    public class MessageTemplateService : IMessageTemplateService
    {
        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _locEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly EmailAccount _defaultEmailAccount;

        public MessageTemplateService(
            SmartDbContext db,
            IApplicationContext appContext,
            ILanguageService languageService,
            ILocalizedEntityService locEntityService,
            IStoreMappingService storeMappingService)
        {
            _db = db;
            _appContext = appContext;
            _languageService = languageService;
            _locEntityService = locEntityService;
            _storeMappingService = storeMappingService;
            _defaultEmailAccount = _db.Set<EmailAccount>().FirstOrDefault(x => x.Email != null);
        }

        public async Task<MessageTemplate> CopyTemplateAsync(MessageTemplate source)
        {
            Guard.NotNull(source, nameof(source));

            var copy = new MessageTemplate
            {
                Name = source.Name,
                To = source.To,
                ReplyTo = source.ReplyTo,
                ModelTypes = source.ModelTypes,
                LastModelTree = source.LastModelTree,
                BccEmailAddresses = source.BccEmailAddresses,
                Subject = source.Subject,
                Body = source.Body,
                IsActive = source.IsActive,
                EmailAccountId = source.EmailAccountId,
                LimitedToStores = source.LimitedToStores
                // INFO: we do not copy attachments
            };

            _db.MessageTemplates.Add(copy);
            await _db.SaveChangesAsync();

            var languages = await _languageService.GetAllLanguagesAsync(true);

            // Localization
            foreach (var lang in languages)
            {
                var bccEmailAddresses = source.GetLocalized(x => x.BccEmailAddresses, lang, false, false);
                if (bccEmailAddresses.HasValue())
                    await _locEntityService.ApplyLocalizedValueAsync(copy, x => x.BccEmailAddresses, bccEmailAddresses, lang.Id);

                var subject = source.GetLocalized(x => x.Subject, lang, false, false);
                if (subject.HasValue())
                    await _locEntityService.ApplyLocalizedValueAsync(copy, x => x.Subject, subject, lang.Id);

                var body = source.GetLocalized(x => x.Body, lang, false, false);
                if (body.HasValue())
                    await _locEntityService.ApplyLocalizedValueAsync(copy, x => x.Body, subject, lang.Id);

                var emailAccountId = source.GetLocalized(x => x.EmailAccountId, lang, false, false);
                if (emailAccountId > 0)
                    await _locEntityService.ApplyLocalizedValueAsync(copy, x => x.EmailAccountId, emailAccountId, lang.Id);
            }

            // Store mappings
            var selectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(source);
            await _storeMappingService.ApplyStoreMappingsAsync(copy, selectedStoreIds);

            // Save now
            await _db.SaveChangesAsync();

            return copy;
        }

        public MessageTemplate LoadTemplate(string templateName, string culture, string rootPath = null)
        {
            Guard.NotEmpty(templateName, nameof(templateName));
            Guard.NotEmpty(culture, nameof(culture));

            var root = _appContext.ContentRoot;
            var dir = ResolveTemplateDirectory(culture, rootPath);
            var file = root.GetFile(PathUtility.Join(dir.SubPath, templateName + ".xml"));

            if (!file.Exists)
            {
                throw new FileNotFoundException($"File '{file.SubPath}' does not exist.");
            }

            return DeserializeTemplate(file);
        }

        public IEnumerable<MessageTemplate> LoadAllTemplates(string culture, string rootPath = null)
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

        public MessageTemplate DeserializeTemplate(string xml, string templateName)
        {
            Guard.NotEmpty(xml, nameof(xml));
            Guard.NotEmpty(templateName, nameof(templateName));

            var template = DeserializeDocument(XDocument.Parse(xml));
            template.Name = templateName;
            return template;
        }

        public XmlDocument SaveTemplate(MessageTemplate template, string culture)
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
            var dir = root.GetDirectory(PathUtility.Join("/App_Data/EmailTemplates", culture));
            dir.Create();

            // File path
            var filePath = PathUtility.Join(dir.SubPath, template.Name + ".xml");

            var xml = Prettifier.PrettifyXML(doc.OuterXml);
            root.WriteAllText(filePath, xml);

            return doc;
        }

        public async Task ImportAllTemplatesAsync(string culture, string rootPath = null)
        {
            var sourceTemplates = LoadAllTemplates(culture, rootPath);
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
                testPaths.Insert(1, culture[..2]);
            }

            foreach (var path in testPaths.Select(x => PathUtility.Join(dir.SubPath, x)))
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
