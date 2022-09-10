using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Web;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class CookieManagerViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IGeoCountryLookup _countryLookup;
        private readonly ICookieConsentManager _cookieConsentManager;
        private readonly PrivacySettings _privacySettings;
        private readonly IWebHelper _webHelper;

        public CookieManagerViewComponent(
            SmartDbContext db,
            IGeoCountryLookup countryLookup,
            ICookieConsentManager cookieConsentManager,
            PrivacySettings privacySettings,
            IWebHelper webHelper)
        {
            _db = db;
            _countryLookup = countryLookup;
            _cookieConsentManager = cookieConsentManager;
            _privacySettings = privacySettings;
            _webHelper = webHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_privacySettings.EnableCookieConsent)
            {
                return Empty();
            }

            // If current country doesn't need cookie consent, don't display cookie manager.
            if (!await DisplayForCountryAsync())
            {
                return Empty();
            }

            var cookieData = _cookieConsentManager.GetCookieData();

            if (cookieData != null && !HttpContext.Request.IsAjax())
            {
                return Empty();
            }

            var model = new CookieManagerModel();

            await PrepareCookieManagerModelAsync(model);

            return View(model);
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
            model.CookiesInfos = (await _cookieConsentManager.GetCookieInfosAsync(true)).ToList();

            var cookie = _cookieConsentManager.GetCookieData();

            model.AnalyticsConsent = cookie != null && cookie.AllowAnalytics;
            model.ThirdPartyConsent = cookie != null && cookie.AllowThirdParty;
            model.ModalCookieConsent = _privacySettings.ModalCookieConsent;
        }
    }
}
