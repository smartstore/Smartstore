using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Localization
{
    public partial class XmlResourceManager : IXmlResourceManager
    {
        [GeneratedRegex("^resources.(.+?).xml$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex FileNameRegEx();
        private static readonly Regex _rgFileName = FileNameRegEx();

        private readonly SmartDbContext _db;
        private readonly IRequestCache _requestCache;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;

        public XmlResourceManager(
            SmartDbContext db,
            IRequestCache requestCache,
            ILanguageService languageService,
            ILocalizationService localizationService)
        {
            _db = db;
            _requestCache = requestCache;
            _languageService = languageService;
            _localizationService = localizationService;
        }

        #region Resource Manager

        public virtual async Task<string> ExportResourcesToXmlAsync(Language language)
        {
            Guard.NotNull(language);

            // TODO: (core) Replace XmlTextWriter with Xml Linq approach.

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            var stringWriter = new StringWriter(sb);
            using var xmlWriter = new XmlTextWriter(stringWriter);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Language");
            xmlWriter.WriteAttributeString("Name", language.Name);

            var resources = await _db.LocaleStringResources
                .AsNoTracking()
                .OrderBy(x => x.ResourceName)
                .Where(x => x.LanguageId == language.Id)
                .ToListAsync();

            foreach (var resource in resources)
            {
                if (resource.IsFromPlugin.GetValueOrDefault() == false)
                {
                    xmlWriter.WriteStartElement("LocaleResource");
                    xmlWriter.WriteAttributeString("Name", resource.ResourceName);
                    xmlWriter.WriteElementString("Value", null, resource.ResourceValue);
                    xmlWriter.WriteEndElement();
                }
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();

            return stringWriter.ToString();
        }

        public virtual async Task<int> ImportResourcesFromXmlAsync(
            Language language,
            XmlDocument xmlDocument,
            string rootKey = null,
            bool sourceIsPlugin = false,
            ImportModeFlags mode = ImportModeFlags.Insert | ImportModeFlags.Update,
            bool updateTouchedResources = false)
        {
            Guard.NotNull(language);
            Guard.NotNull(xmlDocument);

            var resources = language.LocaleStringResources.ToDictionarySafe(x => x.ResourceName, StringComparer.OrdinalIgnoreCase);
            var nodes = xmlDocument.SelectNodes(@"//Language/LocaleResource");
            var isDirty = false;

            foreach (var xel in nodes.Cast<XmlElement>())
            {
                string name = xel.GetAttribute("Name").TrimSafe();
                string value = string.Empty;
                var valueNode = xel.SelectSingleNode("Value");
                if (valueNode != null)
                    value = valueNode.InnerText;

                if (string.IsNullOrEmpty(name))
                    continue;

                if (rootKey.HasValue())
                {
                    if (!xel.GetAttributeText("AppendRootKey").EqualsNoCase("false"))
                        name = "{0}.{1}".FormatCurrent(rootKey, name);
                }

                if (resources.TryGetValue(name, out var resource))
                {
                    if (mode.HasFlag(ImportModeFlags.Update))
                    {
                        if (updateTouchedResources || !resource.IsTouched.GetValueOrDefault())
                        {
                            if (value != resource.ResourceValue)
                            {
                                resource.ResourceValue = value;
                                resource.IsTouched = null;
                                isDirty = true;
                            }
                        }
                    }
                }
                else
                {
                    if (mode.HasFlag(ImportModeFlags.Insert))
                    {
                        var newResource = new LocaleStringResource
                        {
                            LanguageId = language.Id,
                            ResourceName = name,
                            ResourceValue = value,
                            IsFromPlugin = sourceIsPlugin
                        };

                        _db.LocaleStringResources.Add(newResource);
                        resources[name] = newResource;
                        isDirty = true;
                    }
                }
            }

            if (isDirty)
            {
                return await _db.SaveChangesAsync();
            }

            return 0;
        }

        public virtual async Task ImportModuleResourcesFromXmlAsync(
            IModuleDescriptor moduleDescriptor,
            IList<LocaleStringResource> targetList = null,
            bool updateTouchedResources = true,
            List<Language> filterLanguages = null)
        {
            var directory = moduleDescriptor.ContentRoot.GetDirectory("Localization");

            if (!directory.Exists)
                return;

            if (targetList == null && updateTouchedResources && _localizationService != null)
            {
                await _localizationService.DeleteLocaleStringResourcesAsync(moduleDescriptor.ResourceRootKey);
            }

            var unprocessedLanguages = new List<Language>();

            var defaultLanguageId = _languageService != null
                ? await _languageService.GetMasterLanguageIdAsync()
                : (await _db.Languages.FirstOrDefaultAsync()).Id;
            var languages = filterLanguages ?? await _requestCache.GetAsync("db.lang.all.tracked", () => _db.Languages.ToListAsync());

            string code = null;
            foreach (var language in languages)
            {
                code = await ImportModuleResourcesForLanguageAsync(
                    language,
                    null,
                    directory,
                    moduleDescriptor.ResourceRootKey,
                    targetList,
                    updateTouchedResources,
                    false);

                if (code == null)
                {
                    unprocessedLanguages.Add(language);
                }
            }

            if (filterLanguages == null && unprocessedLanguages.Count > 0)
            {
                // There were unprocessed languages (no corresponding resource file could be found).
                // In order for GetResource() to be able to gracefully fallback to the default language's resources,
                // we need to import resources for the current default language....
                var processedLanguages = languages.Except(unprocessedLanguages).ToList();
                if (!processedLanguages.Any(x => x.Id == defaultLanguageId))
                {
                    // ...but only if no resource file could be mapped to the default language before,
                    // namely because in this case the following operation would be redundant.
                    var defaultLanguage = await _db.Languages.FindByIdAsync(defaultLanguageId);
                    if (defaultLanguage != null)
                    {
                        await ImportModuleResourcesForLanguageAsync(
                            defaultLanguage,
                            "en-us",
                            directory,
                            moduleDescriptor.ResourceRootKey,
                            targetList,
                            updateTouchedResources,
                            true);
                    }
                }
            }

            try
            {
                var hasher = CreateModuleResourcesHasher(moduleDescriptor);
                hasher?.Persist();
            }
            catch
            {
            }
        }

        public virtual DirectoryHasher CreateModuleResourcesHasher(IModuleDescriptor moduleDescriptor)
        {
            try
            {
                return moduleDescriptor.ContentRoot.GetDirectoryHasher("Localization", "resources.*.xml");
            }
            catch
            {
                return null;
            }
        }

        public virtual XmlDocument FlattenResourceFile(XmlDocument source)
        {
            Guard.NotNull(source);

            if (source.SelectNodes("//Children").Count == 0)
            {
                // the document contains absolutely NO nesting,
                // so don't bother parsing.
                return source;
            }

            var resources = new List<LocaleStringResourceParent>();

            foreach (XmlNode resNode in source.SelectNodes(@"//Language/LocaleResource"))
            {
                resources.Add(new LocaleStringResourceParent(resNode));
            }

            resources.Sort((x1, x2) => x1.ResourceName.CompareTo(x2.ResourceName));

            foreach (var resource in resources)
            {
                RecursivelySortChildrenResource(resource);
            }

            using var sbp = StringBuilderPool.Instance.Get(out var sb);
            using (var writer = XmlWriter.Create(sb))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Language", "");

                writer.WriteStartAttribute("Name", "");
                writer.WriteString(source.SelectSingleNode(@"//Language").Attributes["Name"].InnerText.Trim());
                writer.WriteEndAttribute();

                foreach (var resource in resources)
                {
                    RecursivelyWriteResource(resource, writer, null);
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
            }

            var result = new XmlDocument();
            result.LoadXml(sb.ToString());

            return result;
        }

        #endregion

        #region Resource Manager Utils

        private async Task<string> ImportModuleResourcesForLanguageAsync(
            Language language,
            string fileCode,
            IDirectory directory,
            string resourceRootKey,
            IList<LocaleStringResource> targetList,
            bool updateTouchedResources,
            bool canFallBackToAnyResourceFile)
        {
            var fileNamePattern = "resources.{0}.xml";
            var fs = directory.FileSystem;

            var codeCandidates = GetResourceFileCodeCandidates(
                fileCode ?? language.LanguageCulture,
                directory,
                canFallBackToAnyResourceFile);

            string path = null;
            string code = null;

            foreach (var candidate in codeCandidates)
            {
                var pathCandidate = PathUtility.Join(directory.SubPath, fileNamePattern.FormatInvariant(candidate));
                if (fs.FileExists(pathCandidate))
                {
                    code = candidate;
                    path = pathCandidate;
                    break;
                }
            }

            if (code != null)
            {
                var doc = new XmlDocument();

                doc.Load(fs.MapPath(path));
                doc = FlattenResourceFile(doc);

                if (targetList == null)
                {
                    await ImportResourcesFromXmlAsync(language, doc, resourceRootKey, true, updateTouchedResources: updateTouchedResources);
                }
                else
                {
                    var nodes = doc.SelectNodes(@"//Language/LocaleResource");
                    foreach (XmlNode node in nodes)
                    {
                        var valueNode = node.SelectSingleNode("Value");
                        var res = new LocaleStringResource
                        {
                            ResourceName = node.Attributes["Name"].InnerText.Trim(),
                            ResourceValue = (valueNode == null ? "" : valueNode.InnerText),
                            LanguageId = language.Id,
                            IsFromPlugin = true
                        };

                        if (res.ResourceName.HasValue())
                        {
                            targetList.Add(res);
                        }
                    }
                }
            }

            return code;
        }

        private static IEnumerable<string> GetResourceFileCodeCandidates(string code, IDirectory directory, bool canFallBackToAnyResourceFile)
        {
            // exact match (de-DE)
            yield return code;

            // neutral culture (de)
            var ci = CultureInfo.GetCultureInfo(code);
            if (ci.Parent != null && !ci.IsNeutralCulture)
            {
                code = ci.Parent.Name;
                yield return code;
            }

            // any other region with same language (de-*)
            foreach (var fi in directory.EnumerateFiles($"resources.{code}-*.xml"))
            {
                code = _rgFileName.Match(fi.Name).Groups[1].Value;
                if (CultureHelper.IsValidCultureCode(code))
                {
                    yield return code;
                    yield break;
                }
            }

            if (canFallBackToAnyResourceFile)
            {
                foreach (var fi in directory.EnumerateFiles("resources.*.xml"))
                {
                    code = _rgFileName.Match(fi.Name).Groups[1].Value;
                    if (CultureHelper.IsValidCultureCode(code))
                    {
                        yield return code;
                        yield break;
                    }
                }
            }
        }

        private static void RecursivelyWriteResource(LocaleStringResourceParent resource, XmlWriter writer, bool? parentAppendRootKey)
        {
            // The value isn't actually used, but the name is used to create a namespace.
            if (resource.IsPersistable)
            {
                writer.WriteStartElement("LocaleResource", "");

                writer.WriteStartAttribute("Name", "");
                writer.WriteString(resource.NameWithNamespace);
                writer.WriteEndAttribute();

                if (resource.AppendRootKey.HasValue)
                {
                    writer.WriteStartAttribute("AppendRootKey", "");
                    writer.WriteString(resource.AppendRootKey.Value ? "true" : "false");
                    writer.WriteEndAttribute();
                    parentAppendRootKey = resource.AppendRootKey;
                }
                else if (parentAppendRootKey.HasValue)
                {
                    writer.WriteStartAttribute("AppendRootKey", "");
                    writer.WriteString(parentAppendRootKey.Value ? "true" : "false");
                    writer.WriteEndAttribute();
                }

                writer.WriteStartElement("Value", "");
                writer.WriteString(resource.ResourceValue);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            foreach (var child in resource.ChildLocaleStringResources)
            {
                RecursivelyWriteResource(child, writer, resource.AppendRootKey ?? parentAppendRootKey);
            }
        }

        private static void RecursivelySortChildrenResource(LocaleStringResourceParent resource)
        {
            ArrayList.Adapter((IList)resource.ChildLocaleStringResources).Sort(new ComparisonComparer<LocaleStringResourceParent>((x1, x2) => x1.ResourceName.CompareTo(x2.ResourceName)));

            foreach (var child in resource.ChildLocaleStringResources)
            {
                RecursivelySortChildrenResource(child);
            }
        }

        private class LocaleStringResourceParent : LocaleStringResource
        {
            public LocaleStringResourceParent(XmlNode localStringResource, string nameSpace = "")
            {
                Namespace = nameSpace;
                var resNameAttribute = localStringResource.Attributes["Name"];
                var resValueNode = localStringResource.SelectSingleNode("Value");

                if (resNameAttribute == null)
                {
                    throw new Exception("All language resources must have an attribute Name=\"Value\".");
                }

                var resName = resNameAttribute.Value.Trim();
                if (string.IsNullOrEmpty(resName))
                {
                    throw new Exception("All languages resource attributes 'Name' must have a value.'");
                }

                ResourceName = resName;

                var appendRootKeyAttribute = localStringResource.Attributes["AppendRootKey"];
                if (appendRootKeyAttribute != null)
                {
                    AppendRootKey = appendRootKeyAttribute.Value.ToBool(true);
                }

                if (resValueNode == null || string.IsNullOrEmpty(resValueNode.InnerText.Trim()))
                {
                    IsPersistable = false;
                }
                else
                {
                    IsPersistable = true;
                    ResourceValue = resValueNode.InnerText.Trim();
                }

                foreach (XmlNode childResource in localStringResource.SelectNodes("Children/LocaleResource"))
                {
                    ChildLocaleStringResources.Add(new LocaleStringResourceParent(childResource, NameWithNamespace));
                }
            }

            public string Namespace { get; set; }

            public IList<LocaleStringResourceParent> ChildLocaleStringResources = new List<LocaleStringResourceParent>();

            public bool IsPersistable { get; set; }

            public bool? AppendRootKey { get; set; }

            public string NameWithNamespace
            {
                get
                {
                    var newNamespace = Namespace;
                    if (!string.IsNullOrEmpty(newNamespace))
                    {
                        newNamespace += ".";
                    }
                    return newNamespace + ResourceName;
                }
            }
        }

        private class ComparisonComparer<T> : IComparer<T>, IComparer
        {
            private readonly Comparison<T> _comparison;

            public ComparisonComparer(Comparison<T> comparison)
            {
                _comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return _comparison(x, y);
            }

            public int Compare(object o1, object o2)
            {
                return _comparison((T)o1, (T)o2);
            }
        }

        #endregion
    }
}
