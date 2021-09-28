using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.Core.Web;
using Smartstore.Utilities;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Controllers
{
    public class CommonController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IGeoCountryLookup _countryLookup;
        private readonly ICookieConsentManager _cookieConsentManager;
        private readonly Lazy<IMediaService> _mediaService;
        private readonly ILanguageService _languageService;
        private readonly UrlPolicy _urlPolicy;
        private readonly IWebHelper _webHelper;
        private readonly IThemeContext _themeContext;
        private readonly IThemeRegistry _themeRegistry;
        private readonly ThemeSettings _themeSettings;
        private readonly SeoSettings _seoSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly PrivacySettings _privacySettings;
        
        public CommonController(
            SmartDbContext db,
            IGeoCountryLookup countryLookup,
            ICookieConsentManager cookieConsentManager,
            Lazy<IMediaService> mediaService,
            ILanguageService languageService,
            UrlPolicy urlPolicy,
            IWebHelper webHelper,
            IThemeContext themeContext, 
            IThemeRegistry themeRegistry, 
            ThemeSettings themeSettings,
            SeoSettings seoSettings,
            LocalizationSettings localizationSettings,
            PrivacySettings privacySettings)
        {
            _db = db;
            _countryLookup = countryLookup;
            _cookieConsentManager = cookieConsentManager;
            _mediaService = mediaService;
            _languageService = languageService;
            _urlPolicy = urlPolicy;
            _webHelper = webHelper;
            _themeContext = themeContext;
            _themeRegistry = themeRegistry;
            _themeSettings = themeSettings;
            _seoSettings = seoSettings;
            _localizationSettings = localizationSettings;
            _privacySettings = privacySettings;
        }

        [CheckStoreClosed(false)]
        [Route("browserconfig.xml")]
        public async Task<IActionResult> BrowserConfigXmlFile()
        {
            var store = Services.StoreContext.CurrentStore;

            if (store.MsTileImageMediaFileId == 0 || store.MsTileColor.IsEmpty())
                return new EmptyResult();

            var mediaService = _mediaService.Value;
            var msTileImage = await mediaService.GetFileByIdAsync(Convert.ToInt32(store.MsTileImageMediaFileId), MediaLoadFlags.AsNoTracking);
            if (msTileImage == null)
                return new EmptyResult();

            XElement root = new (
                "browserconfig",
                new XElement
                (
                    "msapplication",
                    new XElement
                    (
                        "tile",
                        new XElement("square70x70logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeSm, host: string.Empty))),
                        new XElement("square150x150logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeMd, host: string.Empty))),
                        new XElement("square310x310logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeLg, host: string.Empty))),
                        new XElement("wide310x150logo", new XAttribute("src", mediaService.GetUrl(msTileImage, MediaSettings.ThumbnailSizeMd, host: string.Empty))),
                        new XElement("TileColor", store.MsTileColor)
                    )
                )
            );

            var doc = new XDocument(root);
            var xml = doc.ToString(SaveOptions.DisableFormatting);
            return Content(xml, "text/xml");
        }

        [CheckStoreClosed(false)]
        [Route("robots.txt")]
        public async Task<IActionResult> RobotsTextFile()
        {
            #region DisallowPaths

            // TODO: (mh) (core) Check if routes are still the same, when everything is finished. 
            var disallowPaths = SeoSettings.DefaultRobotDisallows;

            // TODO: (mh) (core) Check if routes are still the same, when everything is finished. 
            var localizableDisallowPaths = SeoSettings.DefaultRobotLocalizableDisallows;

            #endregion

            const string newLine = "\r\n"; //Environment.NewLine
            using var psb = StringBuilderPool.Instance.Get(out var sb);
            sb.Append("User-agent: *");
            sb.Append(newLine);
            sb.AppendFormat("Sitemap: {0}", Url.RouteUrl("XmlSitemap", null, Services.StoreContext.CurrentStore.ForceSslForAllPages ? "https" : "http"));
            sb.AppendLine();

            var disallows = disallowPaths.Concat(localizableDisallowPaths);

            if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                var languages = await _db.Languages
                    .AsNoTracking()
                    .ApplyStandardFilter(storeId: Services.StoreContext.CurrentStore.Id)
                    .ToListAsync();

                var masterLanguageId = await _languageService.GetMasterLanguageIdAsync();

                // URLs are localizable. Append SEO code.
                foreach (var language in languages)
                {
                    if (!(_localizationSettings.DefaultLanguageRedirectBehaviour == DefaultLanguageRedirectBehaviour.StripSeoCode && language.Id == masterLanguageId))
                    {
                        disallows = disallows.Concat(localizableDisallowPaths.Select(x => $"/{language.UniqueSeoCode}{x}"));
                    }
                }
            }

            // Append extra allows & disallows.
            disallows = disallows.Concat(_seoSettings.ExtraRobotsDisallows.Select(x => x.Trim()));

            AddRobotsLines(sb, disallows, false);
            AddRobotsLines(sb, _seoSettings.ExtraRobotsAllows.Select(x => x.Trim()), true);

            return Content(sb.ToString(), "text/plain");
        }

        /// <summary>
        /// Adds Allow & Disallow lines to robots.txt .
        /// </summary>
        /// <param name="lines">Lines to add.</param>
        /// <param name="allow">Specifies whether new lines are Allows or Disallows.</param>
        private static void AddRobotsLines(StringBuilder sb, IEnumerable<string> lines, bool allow)
        {
            // Append all lowercase variants (at least Google is case sensitive).
            lines = lines.Union(lines.Select(x => x.ToLower()));

            foreach (var line in lines)
            {
                sb.AppendFormat($"{(allow ? "Allow" : "Disallow")}: {line}");
                sb.Append("\r\n");
            }
        }

        [HttpPost]
        public IActionResult ChangeTheme(string themeName, string returnUrl = null)
        {
            if (!_themeSettings.AllowCustomerToSelectTheme || (themeName.HasValue() && !_themeRegistry.ContainsTheme(themeName)))
            {
                return NotFound();
            }

            _themeContext.WorkingThemeName = themeName;

            if (HttpContext.Request.IsAjaxRequest())
            {
                return Json(new { Success = true });
            }

            return RedirectToReferrer(returnUrl);
        }

        [LocalizedRoute("/currency-selected/{customerCurrency:int}", Name = "ChangeCurrency")]
        public async Task<IActionResult> CurrencySelected(int customerCurrency, string returnUrl = null)
        {
            var currency = await _db.Currencies.FindByIdAsync(customerCurrency);
            if (currency != null)
            {
                Services.WorkContext.WorkingCurrency = currency;
            }

            return RedirectToReferrer(returnUrl);
        }

        [CheckStoreClosed(false)]
        [LocalizedRoute("/set-language/{langid:int}", Name = "ChangeLanguage")]
        public async Task<IActionResult> SetLanguage(int langid, string returnUrl = "")
        {
            var language = await _db.Languages.FindByIdAsync(langid, false);
            if (language != null && language.Published)
            {
                Services.WorkContext.WorkingLanguage = language;
            }

            var helper = new LocalizedUrlHelper(Request.PathBase, returnUrl ?? string.Empty);
            
            if (_urlPolicy.LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                // Don't prepend culture code if it is master language and master is prefixless by configuration.
                if (language.UniqueSeoCode != _urlPolicy.DefaultCultureCode || _urlPolicy.LocalizationSettings.DefaultLanguageRedirectBehaviour == DefaultLanguageRedirectBehaviour.PrependSeoCodeAndRedirect)
                {
                    helper.PrependCultureCode(Services.WorkContext.WorkingLanguage.UniqueSeoCode, true);
                }
            }

            returnUrl = helper.FullPath;

            return RedirectToReferrer(returnUrl);
        }

        // TODO: (mh) (core) Implement GetUnreadPrivateMessages in forum module

        #region CookieManager

        [LocalizedRoute("/cookiemanager", Name = "CookieManager")]
        public async Task<IActionResult> CookieManager()
        {
            if (!_privacySettings.EnableCookieConsent)
            {
                return new EmptyResult();
            }

            // If current country doesn't need cookie consent, don't display cookie manager.
            if (!await DisplayForCountryAsync())
            {
                return new EmptyResult();
            }

            var cookieData = _cookieConsentManager.GetCookieData();

            if (cookieData != null && !HttpContext.Request.IsAjaxRequest())
            {
                return new EmptyResult();
            }

            var model = new CookieManagerModel();

            await PrepareCookieManagerModelAsync(model);

            return PartialView(model);
        }

        private async Task<bool> DisplayForCountryAsync()
        {
            var ipAddress = _webHelper.GetClientIpAddress();
            var lookUpCountryResponse = _countryLookup.LookupCountry(ipAddress);
            if (lookUpCountryResponse?.IsoCode == null)
            {
                // No country was found (e.g. localhost), so we better return true.
                return true;
            }

            var country = await _db.Countries
                .AsNoTracking()
                .ApplyIsoCodeFilter(lookUpCountryResponse.IsoCode)
                .FirstOrDefaultAsync();
            
            if (country != null && country.DisplayCookieManager)
            {
                // Country was configured to display cookie manager.
                return true;
            }

            return false;
        }

        private async Task PrepareCookieManagerModelAsync(CookieManagerModel model)
        {
            // Get cookie infos from plugins.
            model.CookiesInfos = (await _cookieConsentManager.GetAllCookieInfosAsync(true)).ToList();

            var cookie = _cookieConsentManager.GetCookieData();

            model.AnalyticsConsent = cookie != null && cookie.AllowAnalytics;
            model.ThirdPartyConsent = cookie != null && cookie.AllowThirdParty;
            model.ModalCookieConsent = _privacySettings.ModalCookieConsent;
        }

        [HttpPost]
        public ActionResult SetCookieManagerConsent(CookieManagerModel model)
        {
            if (model.AcceptAll)
            {
                model.AnalyticsConsent = true;
                model.ThirdPartyConsent = true;
            }

            _cookieConsentManager.SetConsentCookie(model.AnalyticsConsent, model.ThirdPartyConsent);

            if (!HttpContext.Request.IsAjaxRequest())
            {
                return RedirectToReferrer();
            }

            return Json(new { Success = true });
        }

        #endregion
    }
}
