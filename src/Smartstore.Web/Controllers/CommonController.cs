using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Caching;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.Http;
using Smartstore.Utilities;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Controllers
{
    public class CommonController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ICookieConsentManager _cookieConsentManager;
        private readonly Lazy<IMediaService> _mediaService;
        private readonly ILanguageService _languageService;
        private readonly IThemeContext _themeContext;
        private readonly IThemeRegistry _themeRegistry;
        private readonly ICacheManager _cache;
        private readonly ThemeSettings _themeSettings;
        private readonly SeoSettings _seoSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly IRouteHelper _routeHelper;

        public CommonController(
            SmartDbContext db,
            ICookieConsentManager cookieConsentManager,
            Lazy<IMediaService> mediaService,
            ILanguageService languageService,
            IThemeContext themeContext,
            IThemeRegistry themeRegistry,
            ICacheManager cache,
            ThemeSettings themeSettings,
            SeoSettings seoSettings,
            LocalizationSettings localizationSettings,
            IRouteHelper routeHelper)
        {
            _db = db;
            _cookieConsentManager = cookieConsentManager;
            _mediaService = mediaService;
            _languageService = languageService;
            _themeContext = themeContext;
            _themeRegistry = themeRegistry;
            _cache = cache;
            _themeSettings = themeSettings;
            _seoSettings = seoSettings;
            _localizationSettings = localizationSettings;
            _routeHelper = routeHelper;
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

            XElement root = new(
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

            var disallowPaths = SeoSettings.DefaultRobotDisallows;
            var localizableDisallowPaths = _routeHelper.EnumerateDisallowedRobotPaths();

            #endregion

            var sitemapUrl = WebHelper.GetAbsoluteUrl(Url.Content("sitemap.xml"), Request, true, Services.StoreContext.CurrentStore.SupportsHttps() ? "https" : "http");
            using var psb = StringBuilderPool.Instance.Get(out var sb);
            sb.Append("User-agent: *");
            sb.AppendLine();
            sb.AppendFormat("Sitemap: {0}", sitemapUrl);
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

            // Append custom lines
            if (_seoSettings.ExtraRobotsLines.HasValue())
            {
                sb.Append(_seoSettings.ExtraRobotsLines);
            }

            return Content(sb.ToString(), "text/plain");
        }

        /// <summary>
        /// Adds Allow & Disallow lines to robots.txt
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
                sb.AppendLine();
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

            if (HttpContext.Request.IsAjax())
            {
                return Json(new { Success = true });
            }

            return RedirectToReferrer(returnUrl);
        }

        [LocalizedRoute("/currency-selected/{customerCurrency:int}", Name = "ChangeCurrency")]
        public async Task<IActionResult> CurrencySelected(int customerCurrency, string returnUrl = null)
        {
            var currency = await _db.Currencies.FindByIdAsync(customerCurrency);
            if (currency == null || !currency.Published)
            {
                return NotFound();
            }

            Services.WorkContext.WorkingCurrency = currency;

            return RedirectToReferrer(returnUrl);
        }

        [CheckStoreClosed(false)]
        [LocalizedRoute("/set-language/{langid:int}", Name = "ChangeLanguage")]
        public async Task<IActionResult> SetLanguage(int langid, string returnUrl = "")
        {
            var language = await _db.Languages.FindByIdAsync(langid, false);
            if (language == null || !language.Published)
            {
                return NotFound();
            }

            Services.WorkContext.WorkingLanguage = language;

            var helper = new LocalizedUrlHelper(Request.PathBase, returnUrl ?? string.Empty);
            var urlPolicy = HttpContext.GetUrlPolicy();

            if (urlPolicy != null && urlPolicy.LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                // Don't prepend culture code if it is master language and master is prefixless by configuration.
                if (language.UniqueSeoCode != urlPolicy.DefaultCultureCode || urlPolicy.LocalizationSettings.DefaultLanguageRedirectBehaviour == DefaultLanguageRedirectBehaviour.PrependSeoCodeAndRedirect)
                {
                    helper.PrependCultureCode(Services.WorkContext.WorkingLanguage.UniqueSeoCode, true);
                }
            }

            returnUrl = helper.FullPath;

            return RedirectToReferrer(returnUrl);
        }

        /// <summary>
        /// This action method gets called via an AJAX request.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> StatesByCountryId(string countryId, bool addEmptyStateIfRequired)
        {
            // This should never happen. But just in case we return an empty List to don't throw in frontend.
            if (!countryId.HasValue())
            {
                return Json(new List<SelectListItem>());
            }

            var cacheKey = string.Format(ModelCacheInvalidator.STATEPROVINCES_BY_COUNTRY_MODEL_KEY, countryId, addEmptyStateIfRequired, Services.WorkContext.WorkingLanguage.Id);
            var cacheModel = await _cache.GetAsync(cacheKey, async () =>
            {
                var stateProvinces = await _db.StateProvinces.GetStateProvincesByCountryIdAsync(Convert.ToInt32(countryId));
                var result = stateProvinces.ToSelectListItems() ?? new List<SelectListItem>();

                if (addEmptyStateIfRequired && result.Count == 0)
                {
                    result.Add(new SelectListItem { Text = T("Address.OtherNonUS"), Value = "0" });
                }

                return result;
            });

            return Json(cacheModel);
        }

        [DisallowRobot]
        [LocalizedRoute("/cookiemanager", Name = "CookieManager")]
        public IActionResult CookieManager()
        {
            return ViewComponent("CookieManager");
        }

        [HttpPost]
        public IActionResult SetCookieManagerConsent(CookieManagerModel model)
        {
            if (model.AcceptAll)
            {
                model.AnalyticsConsent = true;
                model.ThirdPartyConsent = true;
                model.AdUserDataConsent = true;
                model.AdPersonalizationConsent = true;
            }

            _cookieConsentManager.SetConsentCookie(model.AnalyticsConsent, model.ThirdPartyConsent, model.AdUserDataConsent, model.AdPersonalizationConsent);

            if (!HttpContext.Request.IsAjax())
            {
                return RedirectToReferrer();
            }

            return Json(new { Success = true });
        }
    }
}