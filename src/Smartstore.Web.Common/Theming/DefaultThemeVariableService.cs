using System.Dynamic;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.Core.Theming;
using Smartstore.Web.Bundling;
using Smartstore.Web.Bundling.Processors;

namespace Smartstore.Web.Theming
{
    public class ThemeValidationResult
    {
        public bool IsValid => Exception == null;
        public Exception Exception { get; set; }
        public string Content { get; set; }
    }

    public class DefaultThemeVariableService : IThemeVariableService
    {
        private const string THEMEVARS_BY_THEME_KEY = "Smartstore:themevars:theme-{0}-{1}";

        private readonly SmartDbContext _db;
        private readonly IThemeRegistry _themeRegistry;
        private readonly IRequestCache _requestCache;
        private readonly IBundleBuilder _bundleBuilder;
        private readonly IBundleCollection _bundles;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultThemeVariableService(
            SmartDbContext db,
            IThemeRegistry themeRegistry,
            IRequestCache requestCache,
            IBundleBuilder bundleBuilder,
            IBundleCollection bundles,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _themeRegistry = themeRegistry;
            _requestCache = requestCache;
            _bundleBuilder = bundleBuilder;
            _bundles = bundles;
            _httpContextAccessor = httpContextAccessor;
        }

        public virtual async Task<ExpandoObject> GetThemeVariablesAsync(string themeName, int storeId)
        {
            if (themeName.IsEmpty() || !_themeRegistry.ContainsTheme(themeName))
            {
                return null;
            }

            string key = string.Format(THEMEVARS_BY_THEME_KEY, themeName, storeId);
            var result = await _requestCache.GetAsync(key, async () =>
            {
                var dbVars = await _db.ThemeVariables
                    .AsNoTracking()
                    .Where(v => v.StoreId == storeId && v.Theme == themeName)
                    .ToDictionaryAsync(x => x.Name, x => (object)x.Value);

                return MergeThemeVariables(_themeRegistry.GetThemeDescriptor(themeName), dbVars);
            });

            return result;
        }

        /// <summary>
        /// Merges <paramref name="variables"/> with preconfigured variables from <paramref name="descriptor"/>. 
        /// </summary>
        private ExpandoObject MergeThemeVariables(ThemeDescriptor descriptor, IDictionary<string, object> variables)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            var result = new ExpandoObject();
            var dict = result as IDictionary<string, object>;

            // First get all default (static) var values from descriptor...
            descriptor.Variables.Values.Each(v =>
            {
                dict.Add(v.Name, v.DefaultValue);
            });

            // ...then merge with passed variables
            foreach (var kvp in variables)
            {
                if (kvp.Value != null && dict.ContainsKey(kvp.Key))
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        public virtual async Task<int> SaveThemeVariablesAsync(string themeName, int storeId, IDictionary<string, object> variables)
        {
            Guard.NotEmpty(themeName, nameof(themeName));
            Guard.Against<ArgumentException>(!_themeRegistry.ContainsTheme(themeName), "The theme '{0}' does not exist in the registry.".FormatInvariant(themeName));
            Guard.NotNull(variables, nameof(variables));

            if (!variables.Any())
                return 0;

            var descriptor = _themeRegistry.GetThemeDescriptor(themeName);
            if (descriptor == null)
            {
                throw new ArgumentException("Theme '{0}' does not exist".FormatInvariant(themeName), nameof(themeName));
            }

            // Validate before save and ensure that Sass compiler does not throw with updated variables
            var mergedVariables = MergeThemeVariables(descriptor, variables);
            var validationResult = await ValidateThemeAsync(descriptor, storeId, mergedVariables);

            if (!validationResult.IsValid)
            {
                throw new ThemeValidationException(validationResult.Exception.ToAllMessages(), variables);
            }

            // Save
            var result = await SaveThemeVariablesInternal(descriptor, storeId, variables);

            return result.TouchedVariablesCount;
        }

        /// <summary>
        /// Validates the result SASS file by compiling the theme bundle using the given <paramref name="variables"/>.
        /// </summary>
        /// <param name="theme">Theme</param>
        /// <param name="storeId">Stored Id</param>
        /// <param name="variables">The variables to use for compilation.</param>
        protected virtual async Task<ThemeValidationResult> ValidateThemeAsync(ThemeDescriptor theme, int storeId, IDictionary<string, object> variables)
        {
            Guard.NotNull(theme, nameof(theme));
            Guard.NotNull(variables, nameof(variables));

            var themeName = theme.Name.ToLower();
            var route = $"/themes/{themeName}/theme.css";
            var bundle = _bundles.GetBundleFor(route);
            var result = new ThemeValidationResult();

            if (bundle == null)
            {
                return result;
            }

            var cacheKey = new BundleCacheKey
            {
                Key = route,
                Fragments = new Dictionary<string, string>
                {
                    ["Theme"] = themeName,
                    ["StoreId"] = storeId.ToString()
                }
            };

            try
            {
                var clone = new Bundle(bundle);
                clone.Processors.Clear();
                clone.Processors.Add(SassProcessor.Instance);

                var dataTokens = new Dictionary<string, object>
                {
                    ["ThemeVars"] = variables
                };

                var bundleResponse = await _bundleBuilder.BuildBundleAsync(clone, cacheKey, dataTokens);
                result.Content = bundleResponse.Content;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }

            return result;
        }

        private async Task<SaveThemeVariablesResult> SaveThemeVariablesInternal(ThemeDescriptor descriptor, int storeId, IDictionary<string, object> variables)
        {
            var result = new SaveThemeVariablesResult();
            var infos = descriptor.Variables;

            var unsavedVars = new List<string>();
            var savedThemeVars = await _db.ThemeVariables
                .Where(v => v.StoreId == storeId && v.Theme == descriptor.Name)
                .ToDictionaryAsync(x => x.Name);

            bool touched = false;

            foreach (var v in variables)
            {
                if (!infos.TryGetValue(v.Key, out var info))
                {
                    // var not specified in metadata so don't save
                    // TODO: (MC) delete from db also if it exists
                    continue;
                }

                var value = v.Value == null ? string.Empty : v.Value.ToString();

                var savedThemeVar = savedThemeVars.Get(v.Key);
                if (savedThemeVar != null)
                {
                    if (value.IsEmpty() || string.Equals(info.DefaultValue, value, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // it's either null or the default value, so delete
                        _db.ThemeVariables.Remove(savedThemeVar);
                        result.Deleted.Add(savedThemeVar);
                        touched = true;
                    }
                    else
                    {
                        // update entity
                        if (!savedThemeVar.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                        {
                            savedThemeVar.Value = value;
                            result.Updated.Add(savedThemeVar);
                            touched = true;
                        }
                    }
                }
                else
                {
                    if (value.HasValue() && !string.Equals(info.DefaultValue, value, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Insert entity (only when not default value)
                        unsavedVars.Add(v.Key);
                        savedThemeVar = new ThemeVariable
                        {
                            Theme = descriptor.Name,
                            Name = v.Key,
                            Value = value,
                            StoreId = storeId
                        };
                        _db.ThemeVariables.Add(savedThemeVar);
                        result.Inserted.Add(savedThemeVar);
                        touched = true;
                    }
                }
            }

            if (touched)
            {
                await _db.SaveChangesAsync();
            }

            return result;
        }

        public virtual Task DeleteThemeVariablesAsync(string themeName, int storeId)
        {
            return DeleteThemeVariablesInternal(themeName, storeId);
        }

        private async Task DeleteThemeVariablesInternal(string themeName, int storeId, bool save = true)
        {
            Guard.NotEmpty(themeName, nameof(themeName));

            var dbVars = await _db.ThemeVariables
                .Where(v => v.StoreId == storeId && v.Theme == themeName)
                .ToListAsync();

            if (dbVars.Any())
            {
                dbVars.Each(v =>
                {
                    _db.ThemeVariables.Remove(v);
                });

                _requestCache.Remove(THEMEVARS_BY_THEME_KEY.FormatInvariant(themeName, storeId));

                if (save)
                {
                    await _db.SaveChangesAsync();
                }
            }
        }

        public virtual Task<int> ImportVariablesAsync(string themeName, int storeId, string configurationXml)
        {
            Guard.NotEmpty(themeName, nameof(themeName));
            Guard.NotEmpty(configurationXml, nameof(configurationXml));

            var dict = new Dictionary<string, object>();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(configurationXml);

            string forTheme = xmlDoc.DocumentElement.GetAttribute("for");
            if (!forTheme.EqualsNoCase(themeName))
            {
                throw new InvalidOperationException("The theme reference in the import file ('{0}') does not match the current theme '{1}'.".FormatCurrent(forTheme.ToSafe(), themeName));
            }

            var xndVars = xmlDoc.DocumentElement.SelectNodes("Var").Cast<XmlElement>();
            foreach (var xel in xndVars)
            {
                string name = xel.GetAttribute("name");
                string value = xel.InnerText;

                if (name.IsEmpty() || value.IsEmpty())
                {
                    continue;
                }

                dict.Add(name, value);
            }

            return SaveThemeVariablesAsync(themeName, storeId, dict);
        }

        public virtual async Task<string> ExportVariablesAsync(string themeName, int storeId)
        {
            Guard.NotEmpty(themeName, nameof(themeName));

            var vars = await GetThemeVariablesAsync(themeName, storeId) as IDictionary<string, object>;

            if (vars == null || !vars.Any())
                return null;

            var infos = _themeRegistry.GetThemeDescriptor(themeName).Variables;

            var sb = new StringBuilder(1000);

            using (var xmlWriter = XmlWriter.Create(sb))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("ThemeVars");
                xmlWriter.WriteAttributeString("for", themeName);

                foreach (var kvp in vars)
                {
                    string name = kvp.Key;
                    string value = kvp.Value.ToString();

                    if (!infos.TryGetValue(name, out var info))
                    {
                        // var not specified in metadata so don't export
                        continue;
                    }

                    xmlWriter.WriteStartElement("Var");
                    xmlWriter.WriteAttributeString("name", name);
                    xmlWriter.WriteAttributeString("type", info.TypeAsString);
                    xmlWriter.WriteString(value);
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }

            return sb.ToString();
        }

        class SaveThemeVariablesResult
        {
            public List<ThemeVariable> Inserted { get; } = new();
            public List<ThemeVariable> Updated { get; } = new();
            public List<ThemeVariable> Deleted { get; } = new();

            public int TouchedVariablesCount => Inserted.Count + Updated.Count + Deleted.Count;
        }
    }
}
