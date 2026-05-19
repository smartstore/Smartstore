using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using DotLiquid.Tags;
using MaxMind.GeoIP2.Model;
using Microsoft.AspNetCore.Mvc.Rendering;
using Org.BouncyCastle.Asn1.X509;
using SixLabors.ImageSharp.ColorSpaces;
using Smartstore.Caching;
using Smartstore.Core.Catalog;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Utilities;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Menus;

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
        private readonly CatalogSettings _catalogSettings;
        private readonly IMenuService _menuService;
        private readonly Lazy<IProviderManager> _providerManager;
        private readonly Lazy<ModuleManager> _moduleManager;
        private readonly PaymentSettings _paymentSettings;
        private readonly SocialSettings _socialSettings;
        private readonly HomePageSettings _homePageSettings;
        private readonly CompanyInformationSettings _companyInformationSettings;
        private readonly ContactDataSettings _contactDataSettings;
        private readonly TaxSettings _taxSettings;

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
            IRouteHelper routeHelper,
            CatalogSettings catalogSettings,
            IMenuService menuService,
            Lazy<IProviderManager> providerManager,
            Lazy<ModuleManager> moduleManager,
            PaymentSettings paymentSettings,
            SocialSettings socialSettings,
            HomePageSettings homePageSettings,
            CompanyInformationSettings companyInformationSettings,
            ContactDataSettings contactDataSettings)
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
            _catalogSettings = catalogSettings;
            _menuService = menuService;
            _providerManager = providerManager;
            _moduleManager = moduleManager;
            _paymentSettings = paymentSettings;
            _socialSettings = socialSettings;
            _homePageSettings = homePageSettings;
            _companyInformationSettings = companyInformationSettings;
            _contactDataSettings = contactDataSettings;
        }

        [CheckStoreClosed(false)]
        [Route("browserconfig.xml"), CrawlerEndpoint]
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
        [Route("robots.txt"), CrawlerEndpoint]
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
            if (lines == null)
            {
                return;
            }

            var directive = allow ? "Allow" : "Disallow";
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                var normalized = line?.Trim();

                if (!normalized.HasValue() || !seen.Add(normalized))
                {
                    continue;
                }

                sb.Append(directive);
                sb.Append(": ");
                sb.AppendLine(normalized);

                // Append lowercase variant (at least Google is case sensitive)
                var lowerNormalized = normalized.ToLowerInvariant();

                if (lowerNormalized != normalized && seen.Add(lowerNormalized))
                {
                    sb.Append(directive);
                    sb.Append(": ");
                    sb.AppendLine(lowerNormalized);
                }
            }
        }

        // TODO: caching!?
        [CheckStoreClosed(false)]
        [Route("llms.txt"), CrawlerEndpoint]
        public async Task<IActionResult> LlmsTextFile()
        {
            using var psb = StringBuilderPool.Instance.Get(out var sb);

            var store = Services.StoreContext.CurrentStore;
            var baseUrl = store.GetBaseUrl().EnsureEndsWith('/');
            sb.AppendLine($"# {store.Name} - LLM Directory");
            sb.AppendLine();

            // Metadata
            var languages = await _db.Languages
                .AsNoTracking()
                .ApplyStandardFilter(false, store.Id)
                .ToListAsync();

            var providers = _providerManager.Value.GetAllProviders<IPaymentMethod>()
                .Where(x => x.IsPaymentProviderEnabled(_paymentSettings));

            sb.AppendLine("## Metadata");
            
            sb.AppendLine($"- Base URL: {baseUrl}");
            if (_homePageSettings.MetaTitle.HasValue())
            {
                sb.AppendLine($"- Title: {_homePageSettings.MetaTitle}");
            }
            if (_homePageSettings.MetaDescription.HasValue())
            {
                sb.AppendLine($"- Title: {_homePageSettings.MetaDescription}");
            }
            if (_companyInformationSettings.CompanyName.HasValue())
            {
                sb.AppendLine($"- Operator: {_companyInformationSettings.CompanyName}");
            }
            if (_companyInformationSettings.CompanyManagementDescription.HasValue())
            {
                sb.AppendLine($"- Legal Representatives: {_companyInformationSettings.CompanyManagementDescription}");
            }

            // Address
            {
                var addressParts = new List<string>();

                var street = _companyInformationSettings.Street;
                var street2 = _companyInformationSettings.Street2;
                var zip = _companyInformationSettings.ZipCode;
                var city = _companyInformationSettings.City;
                var stateName = _companyInformationSettings.StateName;
                var countryId = _companyInformationSettings.CountryId;

                string countryName = null;
                if (countryId > 0)
                {
                    var country = await _db.Countries.FindByIdAsync(countryId, false);
                    countryName = country?.GetLocalized(x => x.Name);
                }

                var streetLine = string.Join(" ", new[] { street, street2 }.Where(x => x.HasValue()));
                var cityLine = string.Join(" ", new[] { zip, city }.Where(x => x.HasValue()));      

                if (streetLine.HasValue()) addressParts.Add(streetLine);
                if (cityLine.HasValue()) addressParts.Add(cityLine);
                if (stateName.HasValue()) addressParts.Add(stateName);
                if (countryName.HasValue()) addressParts.Add(countryName);  

                if (addressParts.Count > 0)
                {
                    sb.AppendLine($"- Address: {string.Join(", ", addressParts)}");
                }
            }

            if (_companyInformationSettings.CommercialRegister.HasValue())
            {
                sb.AppendLine($"- Registered at: {_companyInformationSettings.CommercialRegister}");
            }
            if (_companyInformationSettings.VatId.HasValue())
            {
                sb.AppendLine($"- VAT ID: {_companyInformationSettings.VatId}");
            }

            if (_contactDataSettings.SupportEmailAddress.HasValue())
            {
                sb.AppendLine($"- Support Email: {_contactDataSettings.SupportEmailAddress}");
            }
            if (_contactDataSettings.HotlineTelephoneNumber.HasValue())
            {
                sb.AppendLine($"- Support Phone: {_contactDataSettings.HotlineTelephoneNumber}");
            }

            // TODO

            //            -Target Audience: B2C
            //- Price Display: Gross(Prices include VAT)
            //                oder
            //                - Target Audience: B2B
            //- Price Display: Net(Prices exclude VAT)

            sb.AppendLine($"- Currency: {Services.WorkContext.WorkingCurrency?.CurrencyCode}");
            sb.AppendLine($"- Available Languages: {string.Join(", ", languages.Select(x => x.UniqueSeoCode))}");
            sb.AppendLine($"- Available Payment Methods: {string.Join(", ", providers.Select(x => _moduleManager.Value.GetLocalizedFriendlyName(x.Metadata)))}");
            sb.AppendLine();

            // Main product categories
            var menu = await _menuService.GetMenuAsync("Main");
            if (menu != null)
            {
                var model = await menu.CreateModelAsync(null, ControllerContext);
                var rootChildren = model.Root.Children;

                sb.AppendLine("## Main product categories");
                foreach (var node in rootChildren)
                {
                    var item = node.Value;
                    sb.AppendLine($"- [{item.Text}]({ new Uri(new Uri(baseUrl), item.GenerateUrl(ControllerContext))})");
                }
                sb.AppendLine();
            }

            // Discovery Links
            sb.AppendLine("## Discovery Links");
            sb.AppendLine($"- [Sitemap]({baseUrl}sitemap.xml)");

            if (_catalogSettings.RecentlyAddedProductsEnabled && _catalogSettings.RecentlyAddedProductsNumber > 0)
            {
                sb.AppendLine($"- [Recently added products]({Url.RouteUrl("RecentlyAddedProductsRSS", null, Request.Scheme)})");
            }

            sb.AppendLine($"- [Contact & Support]({Url.RouteUrl("ContactUs", null, Request.Scheme)})");
            sb.AppendLine($"- [Brands]({Url.RouteUrl("ManufacturerList", null, Request.Scheme)})");
            sb.AppendLine($"- [Shipping & Delivery Info]({ new Uri(new Uri(baseUrl), await Url.TopicAsync("ShippingInfo"))})");
            sb.AppendLine($"- [Privacy Policy]({new Uri(new Uri(baseUrl), await Url.TopicAsync("PrivacyInfo"))})");

            // Social media
            if (_socialSettings.FacebookLink.HasValue()) sb.AppendLine($"- [Facebook]({_socialSettings.FacebookLink})");
            if (_socialSettings.TwitterLink.HasValue()) sb.AppendLine($"- [Twitter]({_socialSettings.TwitterLink})");
            if (_socialSettings.InstagramLink.HasValue()) sb.AppendLine($"- [Instagram]({_socialSettings.InstagramLink})");
            if (_socialSettings.TikTokLink.HasValue()) sb.AppendLine($"- [TikTok]({_socialSettings.TikTokLink})");
            if (_socialSettings.YoutubeLink.HasValue()) sb.AppendLine($"- [YouTube]({_socialSettings.YoutubeLink})");
            if (_socialSettings.VimeoLink.HasValue()) sb.AppendLine($"- [Vimeo]({_socialSettings.VimeoLink})");
            if (_socialSettings.PinterestLink.HasValue()) sb.AppendLine($"- [Pinterest]({_socialSettings.PinterestLink})");
            if (_socialSettings.SnapchatLink.HasValue()) sb.AppendLine($"- [Snapchat]({_socialSettings.SnapchatLink})");
            if (_socialSettings.FlickrLink.HasValue()) sb.AppendLine($"- [Flickr]({_socialSettings.FlickrLink})");
            if (_socialSettings.LinkedInLink.HasValue()) sb.AppendLine($"- [LinkedIn]({_socialSettings.LinkedInLink})");
            if (_socialSettings.XingLink.HasValue()) sb.AppendLine($"- [Xing]({_socialSettings.XingLink})");
            if (_socialSettings.TumblrLink.HasValue()) sb.AppendLine($"- [Tumblr]({_socialSettings.TumblrLink})");
            if (_socialSettings.ElloLink.HasValue()) sb.AppendLine($"- [Ello]({_socialSettings.ElloLink})");
            if (_socialSettings.BehanceLink.HasValue()) sb.AppendLine($"- [Behance]({_socialSettings.BehanceLink})");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
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

        [DisallowRobot]
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
        [DisallowRobot]
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
        [HttpPost]
        [LocalizedRoute("/cookiemanager", Name = "CookieManager")]
        public IActionResult CookieManager()
        {
            return ViewComponent("CookieManager");
        }

        [HttpPost]
        public async Task<IActionResult> SetCookieManagerConsent(CookieManagerModel model)
        {
            if (model != null)
            {
                if (model.AcceptAll)
                {
                    model.RequiredConsent = true;
                    model.AnalyticsConsent = true;
                    model.ThirdPartyConsent = true;
                    model.AdUserDataConsent = true;
                    model.AdPersonalizationConsent = true;
                }

                // Info: We don't pass the required value from model.RequiredConsent because the control is disabled and the value is always false
                // but required cookies are always allowed. However, we need to set the consent explicitly because we can't even set required cookies without consent.
                await _cookieConsentManager.SetConsentCookieAsync(true, model.AnalyticsConsent, model.ThirdPartyConsent, model.AdUserDataConsent, model.AdPersonalizationConsent);
            }

            if (!HttpContext.Request.IsAjax())
            {
                return RedirectToReferrer();
            }

            return Json(new { Success = model != null });
        }
    }
}