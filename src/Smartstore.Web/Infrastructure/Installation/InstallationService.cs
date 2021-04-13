using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data.Migrations;
using Smartstore.Engine;

namespace Smartstore.Web.Infrastructure.Installation
{
    public partial class InstallationService : IInstallationService
    {
        const string LanguageCookieName = ".Smart.Installation.Lang";

        private IList<InstallationLanguage> _installLanguages;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApplicationContext _appContext;
        private readonly IEnumerable<Lazy<InvariantSeedData, InstallationAppLanguageMetadata>> _seedDatas;

        public InstallationService(
            IHttpContextAccessor httpContextAccessor,
            IApplicationContext appContext,
            IEnumerable<Lazy<InvariantSeedData, InstallationAppLanguageMetadata>> seedDatas)
        {
            _httpContextAccessor = httpContextAccessor;
            _appContext = appContext;
            _seedDatas = seedDatas;
        }

        #region Installation

        public virtual Task<InstallationResult> Install(InstallationModel model)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Localization

        public virtual string GetResource(string resourceName)
        {
            Guard.NotEmpty(resourceName, nameof(resourceName));
            
            var language = GetCurrentLanguage();
            if (language == null)
                return resourceName;

            if (language.Resources.TryGetValue(resourceName, out var entry) && entry.Value.HasValue())
            {
                return entry.Value;
            }

            return resourceName;
        }

        public virtual InstallationLanguage GetCurrentLanguage()
        {
            var request = _httpContextAccessor?.HttpContext?.Request;
            var cookieLanguageCode = request?.Cookies[LanguageCookieName].EmptyNull();

            // Ensure it's available (it could be deleted since the previous installation)
            var installLanguages = GetInstallationLanguages();

            var language = installLanguages
                .Where(x => x.Code.EqualsNoCase(cookieLanguageCode))
                .FirstOrDefault();

            if (language != null)
                return language;

            // Try to resolve install language from CurrentCulture
            language = installLanguages.Where(MatchLanguageByCurrentCulture).FirstOrDefault();
            if (language != null)
                return language;

            // If we got here, the language code is not found. Let's return the default one
            language = installLanguages.Where(x => x.IsDefault).FirstOrDefault();
            if (language != null)
                return language;

            // Return any available language
            return installLanguages.FirstOrDefault();
        }

        private bool MatchLanguageByCurrentCulture(InstallationLanguage language)
        {
            var curCulture = Thread.CurrentThread.CurrentUICulture;

            if (language.Code.EqualsNoCase(curCulture.IetfLanguageTag))
                return true;

            curCulture = curCulture.Parent;
            if (curCulture != null)
            {
                return language.Code.EqualsNoCase(curCulture.IetfLanguageTag);
            }

            return false;
        }

        public virtual void SaveCurrentLanguage(string languageCode)
        {
            Guard.NotEmpty(languageCode, nameof(languageCode));
            
            var cookies = _httpContextAccessor.HttpContext?.Response?.Cookies;
            if (cookies == null)
            {
                return;
            }

            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(24),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            };

            cookies.Delete(LanguageCookieName, options);
            cookies.Append(LanguageCookieName, languageCode, options);
        }

        public virtual IEnumerable<InstallationAppLanguageMetadata> GetAppLanguages()
        {
            foreach (var data in _seedDatas)
            {
                yield return data.Metadata;
            }
        }

        public Lazy<InvariantSeedData, InstallationAppLanguageMetadata> GetAppLanguage(string culture)
        {
            Guard.NotEmpty(culture, nameof(culture));
            return _seedDatas.FirstOrDefault(x => x.Metadata.Culture.EqualsNoCase(culture));
        }

        public IList<InstallationLanguage> GetInstallationLanguages()
        {
            if (_installLanguages == null)
            {
                _installLanguages = new List<InstallationLanguage>();

                foreach (var file in _appContext.AppDataRoot.EnumerateFiles("Localization/Installation", "*.xml"))
                {
                    var filePath = file.PhysicalPath;
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(filePath);

                    // Get language code
                    var languageCode = string.Empty;

                    // File name format: installation.{languagecode}.xml
                    var r = new Regex(Regex.Escape("installation.") + "(.*?)" + Regex.Escape(".xml"));
                    var matches = r.Matches(file.Name);
                    foreach (Match match in matches)
                    {
                        languageCode = match.Groups[1].Value;
                    } 

                    // Get language friendly name
                    var languageName = xmlDocument.SelectSingleNode(@"//Language").Attributes["Name"].InnerText.Trim();

                    // Is default
                    var isDefaultAttribute = xmlDocument.SelectSingleNode(@"//Language").Attributes["IsDefault"];
                    var isDefault = isDefaultAttribute != null && Convert.ToBoolean(isDefaultAttribute.InnerText.Trim());

                    // Is RTL
                    var isRightToLeftAttribute = xmlDocument.SelectSingleNode(@"//Language").Attributes["IsRightToLeft"];
                    var isRightToLeft = isRightToLeftAttribute != null && Convert.ToBoolean(isRightToLeftAttribute.InnerText.Trim());

                    // Create language
                    var language = new InstallationLanguage
                    {
                        Code = languageCode,
                        Name = languageName,
                        IsDefault = isDefault,
                        IsRightToLeft = isRightToLeft
                    };

                    // Load resources
                    foreach (XmlNode resNode in xmlDocument.SelectNodes(@"//Language/LocaleResource"))
                    {
                        var resNameAttribute = resNode.Attributes["Name"];
                        var resValueNode = resNode.SelectSingleNode("Value");

                        if (resNameAttribute == null)
                            throw new SmartException("All installation resources must have an attribute Name=\"Value\".");

                        var resourceName = resNameAttribute.Value.Trim();
                        if (string.IsNullOrEmpty(resourceName))
                            throw new SmartException("All installation resource attributes 'Name' must have a value.'");

                        if (resValueNode == null)
                            throw new SmartException("All installation resources must have an element \"Value\".");

                        var resourceValue = resValueNode.InnerText.Trim();
                        language.Resources[resourceName] = new InstallationLocaleResource
                        {
                            Name = resourceName,
                            Value = resourceValue
                        };
                    }

                    _installLanguages.Add(language);
                }

                _installLanguages = _installLanguages.OrderBy(x => x.Name).ToList();
            }

            return _installLanguages;
        }

        #endregion
    }
}
