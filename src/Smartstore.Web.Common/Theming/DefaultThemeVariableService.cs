using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Theming;
using Smartstore.Events;
using Smartstore.Http;
using Smartstore.Utilities;
using Smartstore.Web.Bundling;

namespace Smartstore.Web.Theming
{
    public class DefaultThemeVariableService : IThemeVariableService
    {
        private const string THEMEVARS_BY_THEME_KEY = "Smartstore:themevars:theme-{0}-{1}";

        private readonly SmartDbContext _db;
        private readonly IThemeRegistry _themeRegistry;
        private readonly IRequestCache _requestCache;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAssetFileProvider _assetFileProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultThemeVariableService(
            SmartDbContext db,
            IThemeRegistry themeRegistry,
            IRequestCache requestCache,
            IEventPublisher eventPublisher,
            IAssetFileProvider assetFileProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _themeRegistry = themeRegistry;
            _requestCache = requestCache;
            _eventPublisher = eventPublisher;
            _assetFileProvider = assetFileProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ExpandoObject> GetThemeVariablesAsync(string themeName, int storeId)
        {
            if (themeName.IsEmpty())
                return null;

            if (!_themeRegistry.ContainsTheme(themeName))
                return null;

            string key = string.Format(THEMEVARS_BY_THEME_KEY, themeName, storeId);
            return await _requestCache.GetAsync(key, async () =>
            {
                var result = new ExpandoObject();
                var dict = result as IDictionary<string, object>;

                // First get all default (static) var values from manifest...
                var manifest = _themeRegistry.GetThemeManifest(themeName);
                manifest.Variables.Values.Each(v =>
                {
                    dict.Add(v.Name, v.DefaultValue);
                });

                // ...then merge with persisted runtime records
                var dbVars = await _db.ThemeVariables
                    .AsNoTracking()
                    .Where(v => v.StoreId == storeId && v.Theme == themeName)
                    .ToListAsync();

                dbVars.Each(v =>
                {
                    if (v.Value.HasValue() && dict.ContainsKey(v.Name))
                    {
                        dict[v.Name] = v.Value;
                    }
                });

                return result;
            });
        }

        public async Task<int> SaveThemeVariablesAsync(string themeName, int storeId, IDictionary<string, object> variables)
        {
            Guard.NotEmpty(themeName, nameof(themeName));
            Guard.Against<ArgumentException>(!_themeRegistry.ContainsTheme(themeName), "The theme '{0}' does not exist in the registry.".FormatInvariant(themeName));
            Guard.NotNull(variables, nameof(variables));

            if (!variables.Any())
                return 0;

            var manifest = _themeRegistry.GetThemeManifest(themeName);
            if (manifest == null)
            {
                throw new ArgumentException("Theme '{0}' does not exist".FormatInvariant(themeName), nameof(themeName));
            }

            // Get current for later restore on parse error
            var currentVars = await GetThemeVariablesAsync(themeName, storeId);

            // Save
            var result = await SaveThemeVariablesInternal(manifest, storeId, variables);

            if (result.TouchedVariablesCount > 0)
            {
                // Check for parsing error
                string error = await ValidateSassAsync(manifest, storeId);
                if (error.HasValue())
                {
                    // Restore previous vars
                    try
                    {
                        await DeleteThemeVariablesInternal(themeName, storeId, false);
                    }
                    finally
                    {
                        // We do it here to absolutely ensure that this gets called
                        await SaveThemeVariablesInternal(manifest, storeId, currentVars);
                    }

                    throw new ThemeValidationException(error, variables);
                }
            }

            return result.TouchedVariablesCount;
        }

        private async Task<SaveThemeVariablesResult> SaveThemeVariablesInternal(ThemeManifest manifest, int storeId, IDictionary<string, object> variables)
        {
            var result = new SaveThemeVariablesResult();
            var infos = manifest.Variables;

            var unsavedVars = new List<string>();
            var savedThemeVars = await _db.ThemeVariables
                .Where(v => v.StoreId == storeId && v.Theme == manifest.ThemeName)
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
                    if (value.IsEmpty() || String.Equals(info.DefaultValue, value, StringComparison.CurrentCultureIgnoreCase))
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
                            Theme = manifest.ThemeName,
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

        public Task DeleteThemeVariablesAsync(string themeName, int storeId)
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

        public Task<int> ImportVariablesAsync(string themeName, int storeId, string configurationXml)
        {
            Guard.NotEmpty(themeName, nameof(themeName));
            Guard.NotEmpty(configurationXml, nameof(configurationXml));

            var dict = new Dictionary<string, object>();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(configurationXml);

            string forTheme = xmlDoc.DocumentElement.GetAttribute("for");
            if (!forTheme.EqualsNoCase(themeName))
            {
                throw new SmartException("The theme reference in the import file ('{0}') does not match the current theme '{1}'.".FormatCurrent(forTheme.ToSafe(), themeName));
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

        public async Task<string> ExportVariablesAsync(string themeName, int storeId)
        {
            Guard.NotEmpty(themeName, nameof(themeName));

            var vars = await GetThemeVariablesAsync(themeName, storeId) as IDictionary<string, object>;

            if (vars == null || !vars.Any())
                return null;

            var infos = _themeRegistry.GetThemeManifest(themeName).Variables;

            using var psb = StringBuilderPool.Instance.Get(out var sb);

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

        /// <summary>
        /// Validates the result SASS file by calling it's url.
        /// </summary>
        /// <param name="theme">Theme name</param>
        /// <param name="storeId">Stored Id</param>
        /// <returns>The error message when a parsing error occured, <c>null</c> otherwise</returns>
        private async Task<string> ValidateSassAsync(ThemeManifest manifest, int storeId)
        {
            string error = string.Empty;

            if (_httpContextAccessor.HttpContext == null)
            {
                return error;
            }

            // TODO: (core) DefaultThemeVariableService.ValidateSassAsync() --> finalize theme.scss path/bundle convention and make sass validation run.
            var virtualPath = "~/themes/{0}/theme.scss".FormatCurrent(manifest.ThemeName);

            var url = "{0}?storeId={1}&theme={2}&validate=1".FormatInvariant(
                WebHelper.GetAbsoluteUrl(virtualPath, _httpContextAccessor.HttpContext?.Request),
                storeId,
                manifest.ThemeName);

            var request = await WebHelper.CreateHttpRequestForSafeLocalCallAsync(new Uri(url));
            WebResponse response = null;

            try
            {
                response = await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse)
                {
                    var webResponse = (HttpWebResponse)ex.Response;
                    var statusCode = webResponse.StatusCode;

                    if (statusCode == HttpStatusCode.InternalServerError)
                    {
                        // Catch only 500, as this indicates a parsing error.
                        var stream = webResponse.GetResponseStream();

                        using var streamReader = new StreamReader(stream);
                        // Read the content (the error message has been put there)
                        error = await streamReader.ReadToEndAsync();
                        streamReader.Close();
                        stream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                if (response != null)
                    response.Close();
            }

            return error;
        }
    }

    class SaveThemeVariablesResult
    {
        public List<ThemeVariable> Inserted { get; private set; } = new();
        public List<ThemeVariable> Updated { get; private set; } = new();
        public List<ThemeVariable> Deleted { get; private set; } = new();

        public int TouchedVariablesCount => Inserted.Count + Updated.Count + Deleted.Count;
    }
}
