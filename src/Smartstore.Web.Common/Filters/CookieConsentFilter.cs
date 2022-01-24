using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Smartstore.Core.Web;

namespace Smartstore.Core.Identity
{
    public class CookieConsentFilter : IActionFilter, IAsyncResultFilter
    {
        private readonly PrivacySettings _privacySettings;
        private readonly ICookieConsentManager _cookieConsentManager;
        private readonly IUserAgent _userAgent;
        private readonly IWidgetProvider _widgetProvider;
        
        private bool _isProcessableRequest;

        public CookieConsentFilter(
            PrivacySettings privacySettings, 
            ICookieConsentManager cookieConsentManager, 
            IUserAgent userAgent,
            IWidgetProvider widgetProvider)
        {
            _privacySettings = privacySettings;
            _cookieConsentManager = cookieConsentManager;
            _userAgent = userAgent;
            _widgetProvider = widgetProvider;
        }

        private bool IsProcessableRequest(ActionExecutingContext context)
        {
            if (!_privacySettings.EnableCookieConsent)
                return false;

            var request = context.HttpContext?.Request;

            if (request == null)
                return false;

            if (request.IsAjaxRequest())
                return false;

            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _isProcessableRequest = IsProcessableRequest(context);

            if (!_isProcessableRequest)
                return;

            var isLegacy = false;
            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;
            ConsentCookie cookieData = null;

            // Check if the user has a consent cookie.
            var consentCookie = request.Cookies["CookieConsent"];
            if (consentCookie == null)
            {
                // No consent cookie. We first check the Do Not Track header value, this can have the value "0" or "1"
                var doNotTrack = request.Headers.Get("DNT").FirstOrDefault();

                // If we receive a DNT header, we accept its value (0 = give consent, 1 = deny) and do not ask the user anymore.
                if (doNotTrack.HasValue())
                {
                    if (doNotTrack.Equals("0"))
                    {
                        // Tracking consented.
                        _cookieConsentManager.SetConsentCookie(true, true);
                    }
                    else
                    {
                        // Tracking denied.
                        _cookieConsentManager.SetConsentCookie(false, false);
                    }
                }
                else
                {
                    if (_userAgent.IsBot)
                    {
                        // Don't ask consent from search engines, also don't set cookies.
                        _cookieConsentManager.SetConsentCookie(true, true);
                    }
                    else
                    {
                        // First request on the site and no DNT header (we can use session cookie, which is allowed by EU cookie law).
                        // Don't set cookie!
                    }
                }
            }
            else
            {
                // We received a consent cookie
                try
                {
                    cookieData = JsonConvert.DeserializeObject<ConsentCookie>(consentCookie);
                }
                catch { }

                if (cookieData == null)
                {
                    // Cookie was found but could not be converted thus it's a legacy cookie.
                    isLegacy = true;
                    var str = consentCookie;

                    // 'asked' means customer has not consented.
                    // '2' was the Value of legacy enum CookieConsentStatus.Denied
                    if (str.Equals("asked") || str.Equals("2"))
                    {
                        // Remove legacy Cookie & thus show CookieManager.
                        response.Cookies.Delete("CookieConsent");
                    }
                    // 'true' means consented to all cookies.
                    // '1' was the Value of legacy enum CookieConsentStatus.Consented
                    else if (str.Equals("true") || str.Equals("1"))
                    {
                        // Set Cookie with all types allowed.
                        _cookieConsentManager.SetConsentCookie(true, true);
                    }
                }
            }

            if (!isLegacy)
            {
                context.HttpContext.Items["CookieConsent"] = cookieData;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (!context.Result.IsHtmlViewResult())
            {
                await next();
                return;
            }

            var widget = new ComponentWidgetInvoker("CookieManager", null);
            _widgetProvider.RegisterWidget("end", widget);
            
            await next();
        }
    }
}