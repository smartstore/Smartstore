using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Batching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections;
using System.Xml;
using Smartstore.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using Smartstore.Core.DataExchange;
using Smartstore.Data;
using System.Text;
using Smartstore.Utilities;
using Smartstore.Engine;
using System.IO;

namespace Smartstore.Core.Localization
{
    public class LocalizationService : AsyncDbSaveHook<LocaleStringResource>, ILocalizationService
    {
        /// <summary>
        /// 0 = language id
        /// </summary>
        const string CACHE_SEGMENT_KEY = "localization:{0}";
        const string CACHE_SEGMENT_PATTERN = "localization:*";

        private static Regex _rgFileName = new("^resources.(.+?).xml$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IRequestCache _requestCache;
        private readonly IWorkContext _workContext;
        private readonly ILanguageService _languageService;

        private int _notFoundLogCount = 0;
        private int? _defaultLanguageId;

        public LocalizationService(
            SmartDbContext db, 
            ICacheManager cache,
            IRequestCache requestCache,
            IWorkContext workContext,
            ILanguageService languageService)
        {
            _db = db;
            _cache = cache;
            _requestCache = requestCache;
            _workContext = workContext;
            _languageService = languageService;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Cache & Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var langIds = entries
                .Select(x => x.Entity)
                .OfType<LocaleStringResource>()
                .Select(x => x.LanguageId)
                .Distinct()
                .ToArray();

            foreach (var langId in langIds)
            {
                await ClearCacheSegmentAsync(langId);
            }
        }

        protected virtual Dictionary<string, string> GetCacheSegment(int languageId)
        {
            var cacheKey = BuildCacheSegmentKey(languageId);

            return _cache.Get(cacheKey, (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));

                var resources = _db.LocaleStringResources
                    .Where(x => x.LanguageId == languageId)
                    .Select(x => new { x.ResourceName, x.ResourceValue })
                    .ToList();

                var dict = new Dictionary<string, string>(resources.Count);

                foreach (var res in resources)
                {
                    dict[res.ResourceName.ToLowerInvariant()] = res.ResourceValue;
                }

                return dict;
            });
        }

        protected virtual async Task<Dictionary<string, string>> GetCacheSegmentAsync(int languageId)
        {
            var cacheKey = BuildCacheSegmentKey(languageId);

            return await _cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));
                
                var resources = await _db.LocaleStringResources
                    .Where(x => x.LanguageId == languageId)
                    .Select(x => new { x.ResourceName, x.ResourceValue })
                    .ToListAsync();

                var dict = new Dictionary<string, string>(resources.Count);

                foreach (var res in resources)
                {
                    dict[res.ResourceName.ToLowerInvariant()] = res.ResourceValue;
                }

                return dict;
            });
        }

        /// <summary>
        /// Clears the cached resource segment from the cache
        /// </summary>
        /// <param name="languageId">Language Id. If <c>null</c>, segments for all cached languages will be invalidated</param>
        protected virtual Task ClearCacheSegmentAsync(int? languageId = null)
        {
            if (languageId.HasValue && languageId.Value > 0)
            {
                return _cache.RemoveAsync(BuildCacheSegmentKey(languageId.Value));
            }
            else
            {
                return _cache.RemoveByPatternAsync(CACHE_SEGMENT_PATTERN);
            }
        }

        protected virtual string BuildCacheSegmentKey(int languageId)
        {
            return string.Format(CACHE_SEGMENT_KEY, languageId);
        }

        #endregion

        #region LocaleStringResources

        public virtual string GetResource(string resourceKey,
            int languageId = 0,
            bool logIfNotFound = true,
            string defaultValue = "",
            bool returnEmptyIfNotFound = false)
        {
            languageId = languageId > 0 ? languageId : _workContext.WorkingLanguage?.Id ?? 0;
            if (languageId == 0)
            {
                return defaultValue;
            }

            resourceKey = resourceKey.EmptyNull().Trim().ToLowerInvariant();

            var cachedSegment = GetCacheSegment(languageId);
            if (!cachedSegment.TryGetValue(resourceKey, out string result))
            {
                if (logIfNotFound)
                {
                    LogNotFound(resourceKey, languageId);
                }

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    result = defaultValue;
                }
                else
                {
                    // Try fallback to default language
                    if (!_defaultLanguageId.HasValue)
                    {
                        // TODO: (core) I NEVER wanted to do this (.Result), but I don't wanna always repeat myself either.
                        _defaultLanguageId = _languageService.GetDefaultLanguageIdAsync().Result;
                    }

                    var defaultLangId = _defaultLanguageId.Value;
                    if (defaultLangId > 0 && defaultLangId != languageId)
                    {
                        var fallbackResult = GetResource(resourceKey, defaultLangId, false, resourceKey);
                        if (fallbackResult != resourceKey)
                        {
                            result = fallbackResult;
                        }
                    }

                    if (!returnEmptyIfNotFound && result.IsEmpty())
                    {
                        result = resourceKey;
                    }
                }
            }

            return result;
        }

        public virtual async Task<string> GetResourceAsync(string resourceKey,
            int languageId = 0,
            bool logIfNotFound = true,
            string defaultValue = "",
            bool returnEmptyIfNotFound = false)
        {
            languageId = languageId > 0 ? languageId : _workContext.WorkingLanguage?.Id ?? 0;
            if (languageId == 0)
            {
                return defaultValue;
            }   

            resourceKey = resourceKey.EmptyNull().Trim().ToLowerInvariant();

            var cachedSegment = await GetCacheSegmentAsync(languageId).ConfigureAwait(false);
            if (!cachedSegment.TryGetValue(resourceKey, out string result))
            {
                if (logIfNotFound)
                {
                    LogNotFound(resourceKey, languageId);
                }

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    result = defaultValue;
                }
                else
                {
                    // Try fallback to default language
                    if (!_defaultLanguageId.HasValue)
                    {
                        _defaultLanguageId = await _languageService.GetDefaultLanguageIdAsync().ConfigureAwait(false);
                    }

                    var defaultLangId = _defaultLanguageId.Value;
                    if (defaultLangId > 0 && defaultLangId != languageId)
                    {
                        var fallbackResult = await GetResourceAsync(resourceKey, defaultLangId, false, resourceKey).ConfigureAwait(false);
                        if (fallbackResult != resourceKey)
                        {
                            result = fallbackResult;
                        }
                    }

                    if (!returnEmptyIfNotFound && result.IsEmpty())
                    {
                        result = resourceKey;
                    }
                }
            }

            return result;
        }

        private void LogNotFound(string resourceKey, int languageId)
        {
            if (_notFoundLogCount < 50)
            {
                Logger.Warn("Resource string ({0}) does not exist. Language ID = {1}", resourceKey, languageId);
            }
            else if (_notFoundLogCount == 50)
            {
                Logger.Warn("Too many language resources do not exist (> 50). Stopped logging missing resources to prevent performance drop.");
            }

            _notFoundLogCount++;
        }

        public virtual Task<LocaleStringResource> GetLocaleStringResourceByNameAsync(string resourceName)
        {
            if (_workContext.WorkingLanguage != null)
            {
                return GetLocaleStringResourceByNameAsync(resourceName, _workContext.WorkingLanguage.Id);
            }

            return Task.FromResult((LocaleStringResource)null);
        }

        public virtual async Task<LocaleStringResource> GetLocaleStringResourceByNameAsync(string resourceName, int languageId, bool logIfNotFound = true)
        {
            var query = from x in _db.LocaleStringResources
                        orderby x.ResourceName
                        where x.LanguageId == languageId && x.ResourceName == resourceName
                        select x;

            var entity = await query.FirstOrDefaultAsync();

            if (logIfNotFound && entity == null)
            {
                Logger.Warn("Resource string ({0}) not found. Language ID = {1}", resourceName, languageId);
            } 

            return entity;
        }

        public virtual async Task<int> DeleteLocaleStringResourcesAsync(string key, bool keyIsRootKey = true)
        {
            if (key.IsEmpty())
            {
                return 0;
            }
            
            int result = 0;

            try
            {
                var pattern = (key.EndsWith(".") || !keyIsRootKey ? key : key + ".") + "%";
                result = await _db.LocaleStringResources.Where(x => EF.Functions.Like(x.ResourceName, pattern)).BatchDeleteAsync();
                await ClearCacheSegmentAsync(null);
            }
            catch (Exception ex)
            {
                ex.Dump();
            }

            return result;
        }

        #endregion

        #region Resource Manager

        public virtual async Task<string> ExportResourcesToXmlAsync(Language language)
        {
            Guard.NotNull(language, nameof(language));

            // TODO: (core) Replace XmlTextWriter with Xml Linq approach.

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            var stringWriter = new StringWriter(sb);
            var xmlWriter = new XmlTextWriter(stringWriter);

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
                if (resource.IsFromPlugin == false)
                {
                    xmlWriter.WriteStartElement("LocaleResource");
                    xmlWriter.WriteAttributeString("Name", resource.ResourceName);
                    xmlWriter.WriteElementString("Value", null, resource.ResourceValue);
                    xmlWriter.WriteEndElement();
                }
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();

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
                    if (!xel.GetAttributeText("AppendRootKey").IsCaseInsensitiveEqual("false"))
                        name = "{0}.{1}".FormatWith(rootKey, name);
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
                        isDirty = true;
                        _db.LocaleStringResources.Add(
                            new LocaleStringResource
                            {
                                LanguageId = language.Id,
                                ResourceName = name,
                                ResourceValue = value,
                                IsFromPlugin = sourceIsPlugin
                            });
                    }
                }
            }

            if (isDirty)
            {
                int numSaved = await _db.SaveChangesAsync();
                return numSaved;
            }

            return 0;
        }

        public virtual async Task ImportModuleResourcesFromXmlAsync(
            ModuleDescriptor moduleDescriptor,
            IList<LocaleStringResource> targetList = null,
            bool updateTouchedResources = true,
            List<Language> filterLanguages = null)
        {
            var directory = moduleDescriptor.FileProvider.GetDirectory("Localization");

            if (!directory.Exists)
                return;

            if (targetList == null && updateTouchedResources)
            {
                await DeleteLocaleStringResourcesAsync(moduleDescriptor.ResourceRootKey);
            }

            var unprocessedLanguages = new List<Language>();

            var defaultLanguageId = await _languageService.GetDefaultLanguageIdAsync();
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
                hasher.Persist();
            }
            catch { }
        }

        public virtual DirectoryHasher CreateModuleResourcesHasher(ModuleDescriptor moduleDescriptor)
        {
            return moduleDescriptor.FileProvider.GetDirectoryHasher("Localization", "resources.*.xml");
        }

        public virtual XmlDocument FlattenResourceFile(XmlDocument source)
        {
            Guard.NotNull(source, nameof(source));

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
                var pathCandidate = fs.PathCombine(directory.SubPath, fileNamePattern.FormatInvariant(candidate));
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

                doc.Load(path);
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
            var fs = directory.FileSystem;
            foreach (var fi in fs.EnumerateFiles(directory.SubPath, "resources.{0}-*.xml".FormatInvariant(code)))
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
                foreach (var fi in fs.EnumerateFiles(directory.SubPath, "resources.*.xml"))
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
                    throw new SmartException("All language resources must have an attribute Name=\"Value\".");
                }

                var resName = resNameAttribute.Value.Trim();
                if (string.IsNullOrEmpty(resName))
                {
                    throw new SmartException("All languages resource attributes 'Name' must have a value.'");
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