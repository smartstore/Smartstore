using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Smartstore.Core.Web;
using Smartstore.Net;

namespace Smartstore.Core.Identity
{
    public sealed class CookieConsentAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Checks if the shop visitor has already agreed to the use of cookies, and opens the CookieManager if he or she hasn't.
        /// </summary>
        public CookieConsentAttribute()
            : base(typeof(CookieConsentFilter))
        {
        }

        class CookieConsentFilter : IActionFilter, IResultFilter
        {
            // System names of topics that should not display the consent banner (because it would overlay important legal text)
            readonly static string[] UnprocessableTopics = ["ConditionsOfUse", "PrivacyInfo", "Imprint", "Disclaimer"];

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
                if (_privacySettings.CookieConsentRequirement == CookieConsentRequirement.NeverRequired)
                    return false;

                var request = context.HttpContext?.Request;

                if (request == null)
                    return false;

                if (!request.IsNonAjaxGet())
                    return false;

                return true;
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                _isProcessableRequest = IsProcessableRequest(context);

                if (!_isProcessableRequest)
                    return;

                var isLegacy = false;
                var hasLegacyName = false;
                var request = context.HttpContext.Request;
                var response = context.HttpContext.Response;

                ConsentCookie cookieData = null;

                // Check if the user has a consent cookie.
                var consentCookie = request.Cookies[CookieNames.CookieConsent];

                // Try fetch cookie from pre Smartstore 5.0.0
                if (consentCookie == null)
                {
                    consentCookie = request.Cookies["CookieConsent"];
                    hasLegacyName = true;
                }

                if (consentCookie == null)
                {
                    // No consent cookie. We first check the Do Not Track header value, this can have the value "0" or "1"
                    var doNotTrack = request.Headers.Get("DNT").FirstOrDefault();

                    // If we receive a DNT header, we accept its value (0 = give consent, 1 = deny) and do not ask the user anymore.
                    if (doNotTrack.HasValue())
                    {
                        var consented = doNotTrack.Equals("0");

                        // Tracking consented/denied.
                        _cookieConsentManager.SetConsentCookie(consented, consented);
                    }
                    else
                    {
                        if (_userAgent.IsBot())
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
                    catch
                    {
                    }

                    if (cookieData == null)
                    {
                        // Cookie was found but could not be converted thus it's a pre Smartstore 3 legacy cookie.
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
                    else if (hasLegacyName)
                    {
                        // Cookie was found with old name and could be converted thus it's a pre Smartstore 5 and after Smartstore 3 legacy cookie. So let's rename it.
                        // Remove legacy cookie 
                        response.Cookies.Delete("CookieConsent");
                        // Add again with new name
                        _cookieConsentManager.SetConsentCookie(cookieData.AllowAnalytics, cookieData.AllowThirdParty);
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

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if (!_isProcessableRequest)
                    return;

                // Should only run on a full view rendering result or HTML ContentResult.
                if (!context.Result.IsHtmlViewResult())
                {
                    return;
                }

                // Check for topics which are excluded from displaying the CookieManager.
                var routeIdent = context.RouteData.Values.GenerateRouteIdentifier();
                if (routeIdent == "Topic.TopicDetails")
                {
                    if (context.Result is ViewResult vr && vr.Model != null)
                    {
                        var modelType = vr.Model.GetType();
                        if (modelType.GetProperty("SystemName")?.GetValue(vr.Model) is string systemNameValue && UnprocessableTopics.Contains(systemNameValue))
                        {
                            return;
                        }
                    }
                }

                _widgetProvider.RegisterWidget("end", new ComponentWidget("CookieManager", null));
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }
        }
    }
}