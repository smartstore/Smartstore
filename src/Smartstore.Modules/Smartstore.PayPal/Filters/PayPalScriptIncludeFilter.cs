using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Filters
{
    /// <summary>
    /// Renders a script to detect buyer fraud early by collecting the buyer's browser information 
    /// during checkout and passing it to PayPal (must be active for pay per invoice). 
    /// Also renders the PayPal JS SDK standard script & PayPal helper script which contains 
    /// function to initialize Buttons, Hosted Fields and APMs (alternative payment methods).
    /// </summary>
    public class PayPalScriptIncludeFilter : IAsyncActionFilter
    {
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ICommonServices _services;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly PayPalHelper _payPalHelper;
        private readonly PayPalHttpClient _client;
        private readonly ICookieConsentManager _cookieConsentManager;
        private readonly IPageAssetBuilder _pageAssetBuilder;

        public PayPalScriptIncludeFilter(
            PayPalSettings settings,
            IWidgetProvider widgetProvider,
            ICommonServices services,
            ICheckoutStateAccessor checkoutStateAccessor,
            PayPalHelper payPalHelper,
            PayPalHttpClient client,
            ICookieConsentManager cookieConsentManager,
            IPageAssetBuilder pageAssetBuilder)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _services = services;
            _checkoutStateAccessor = checkoutStateAccessor;
            _payPalHelper = payPalHelper;
            _client = client;
            _cookieConsentManager = cookieConsentManager;
            _pageAssetBuilder = pageAssetBuilder;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var isJsSDKMethodEnabled = await _payPalHelper.IsAnyProviderEnabledAsync(
                PayPalConstants.Standard,
                PayPalConstants.CreditCard,
                PayPalConstants.PayLater,
                PayPalConstants.Sepa,
                PayPalConstants.GooglePay);

            var consented = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.Required);

            if (isJsSDKMethodEnabled)
            {
                // If client id or secret haven't been configured yet, don't show button.
                if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
                {
                    await next();
                    return;
                }

                var currency = _services.WorkContext.WorkingCurrency.CurrencyCode;

                var scriptUrl = $"https://www.paypal.com/sdk/js" +
                    $"?client-id={_settings.ClientId}" +
                    $"&currency={currency}" +
                    // Ensures no breaking changes will be applied in SDK.
                    $"&integration-date=2021-04-13";

                // Complete component param according to used providers
                var googlePayEnabled = await _payPalHelper.IsProviderEnabledAsync(PayPalConstants.GooglePay);
                var creditCardEnabled = await _payPalHelper.IsProviderEnabledAsync(PayPalConstants.CreditCard);
                scriptUrl += $"&components=messages,buttons,funding-eligibility{(creditCardEnabled? ",hosted-fields" : "")}{(googlePayEnabled ? ",googlepay" : "")}";

                if (_settings.Intent == PayPalTransactionType.Authorize)
                {
                    scriptUrl += $"&commit=false";
                }

                // paypal & sepa are the default funding options which are always available.
                // INFO: In fact adding paypal, sepa or card as funding option is breaking the integration in Live mode but not in the Sandbox mode.
                scriptUrl += $"&enable-funding={(_settings.UseSandbox ? "sepa," : "")}paylater";

                scriptUrl += $"&intent={_settings.Intent.ToString().ToLower()}";
                scriptUrl += $"&locale={_services.WorkContext.WorkingLanguage.LanguageCulture.Replace("-", "_")}";

                var clientToken = creditCardEnabled ? await GetClientToken(context.HttpContext) : string.Empty;

                //var scriptIncludeTag = new HtmlString($"<script {(consented ? string.Empty : "data-consent=\"required\" data-")}src='{scriptUrl}' data-partner-attribution-id='SmartStore_Cart_PPCP' data-client-token='{clientToken}' async id='paypal-js'></script>");
                var jsSdkScriptIncludeTag = _cookieConsentManager.GenerateScript(consented, CookieType.Required, scriptUrl);
                jsSdkScriptIncludeTag.Attributes["id"] = "paypal-js";
                jsSdkScriptIncludeTag.Attributes["data-partner-attribution-id"] = "SmartStore_Cart_PPCP";
                jsSdkScriptIncludeTag.Attributes["data-client-token"] = clientToken;
                jsSdkScriptIncludeTag.Attributes["async"] = "async";

                _widgetProvider.RegisterHtml("end", jsSdkScriptIncludeTag);

                // Register Google Pay script include.
                if (googlePayEnabled)
                {
                    var googlePayScriptIncludeTag = _cookieConsentManager.GenerateScript(consented, CookieType.Required, "https://pay.google.com/gp/p/js/pay.js");
                    googlePayScriptIncludeTag.Attributes["async"] = "async";
                    _widgetProvider.RegisterHtml("end", googlePayScriptIncludeTag);
                }
            }

            if (!await _payPalHelper.IsProviderEnabledAsync(PayPalConstants.PayUponInvoice))
            {
                await next();
                return;
            }

            // Write fraudnet script include.
            var routeData = context.RouteData;
            var routeId = routeData.Values.GenerateRouteIdentifier();

            // Risk Session Correlation ID / Client Metadata ID has to be unique and invariant to the current checkout.
            // Will be used for create order API call.
            if (!_checkoutStateAccessor.CheckoutState.PaymentData.TryGetValueAs<string>("ClientMetaId", out var clientMetaId))
            {
                clientMetaId = Guid.NewGuid().ToString();
                _checkoutStateAccessor.CheckoutState.PaymentData["ClientMetaId"] = clientMetaId;
            }
            
            var sourceIdentifier = GetSourceIdentifier(_settings.MerchantName, _settings.PayerId, routeId);

            var sb = new StringBuilder();
            sb.Append("<script type='application/json' fncls='fnparams-dede7cc5-15fd-4c75-a9f4-36c430ee3a99'>");
            // INFO: Single quotes (') aren't allowed to delimit strings.
            sb.Append("{\"sandbox\":" + (_settings.UseSandbox ? "true" : "false") + ",\"f\":\"" + clientMetaId + "\",\"s\":\"" + sourceIdentifier + "\" }");
            sb.Append("</script>");

            //sb.Append($"<script type='text/javascript' {(consented ? string.Empty : "data-consent=\"required\" data-")}src='https://c.paypal.com/da/r/fb.js'></script>");
            var scriptIncludeTag = _cookieConsentManager.GenerateScript(consented, CookieType.Required, "https://c.paypal.com/da/r/fb.js");
            sb.Append(scriptIncludeTag.ToHtmlString());

            sb.Append($"<noscript><img src='https://c.paypal.com/v1/r/d/b/ns?f={clientMetaId}&s={sourceIdentifier}&js=0&r=1' /></noscript>");

            _widgetProvider.RegisterHtml("end", new HtmlString(sb.ToString()));

            await next();
        }

        private static string GetSourceIdentifier(string merchantName, string payerId, string routeId)
        {
            string pageType;
            switch (routeId.ToLower())
            {
                case "home.index":
                    pageType = "home-page";
                    break;
                case "catalog.category":
                    pageType = "category-page";
                    break;
                case "product.productdetails":
                    pageType = "product-detail-page";
                    break;
                case "topic.topicdetails":
                    pageType = "topic-page";
                    break;
                case "home.contactus":
                    pageType = "contactus-page";
                    break;
                case "search.search":
                    pageType = "search-result-page";
                    break;
                case "shoppingcart.cart":
                    pageType = "cart-page";
                    break;
                case "checkout.shippingaddress":
                case "checkout.billingaddress":
                case "checkout.shippingmethod":
                case "checkout.paymentmethod":
                case "smartstore.paypalplus":
                case "customer.login":
                case "checkout.confirm":
                case "checkout.completed":
                    pageType = "checkout-page";
                    break;
                default:
                    pageType = "other";
                    break;
            }

            if (routeId.StartsWith("customer."))
            {
                pageType = "account-page";
            }

            return $"{merchantName}_{payerId}_{pageType}";
        }

        /// <summary>
        /// Gets a client token from session or by requesting PayPal REST API.
        /// </summary>
        /// <returns>Client token to be placed as data attribute in PayPal JS script include.</returns>
        private async Task<string> GetClientToken(HttpContext httpContext)
        {
            // Get client token from session if available.
            var session = httpContext.Session;
            
            var clientToken = session.GetString("PayPalClientToken");
            if (clientToken != null)
            {
                // If clientToken is empty, it means that the last attempt to get a client token failed.
                if (clientToken == string.Empty)
                {
                    // Only try to retrive a new token when we've waited 5 minutes for the API to recover.
                    var tokenFailedDate = session.GetString("PayPalTokenFailedDate").Convert<DateTime?>();
                    if (tokenFailedDate.HasValue && (DateTime.UtcNow - tokenFailedDate.Value).TotalMinutes < 5)
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return clientToken;
                }
            }

            try
            {
                // Get client token from PayPal REST API.
                var response = await _client.ExecuteRequestAsync(_client.RequestFactory.GenerateClientToken());
                dynamic jResponse = JObject.Parse(response.Body<object>().ToString());

                clientToken = (string)jResponse.client_token;

                session.SetString("PayPalClientToken", clientToken);
                session.Remove("PayPalTokenFailedDate");
            }
            catch (Exception ex)
            {
                //In case of failure (maybe because the PayPal API is not responding)
                //we set the client token to string.empty and remember the date when the token retrieval failed.
                session.SetString("PayPalClientToken", string.Empty);
                session.SetString("PayPalTokenFailedDate", DateTime.UtcNow.ToStringInvariant());
                Logger.Error(ex);
            }

            return clientToken;
        }
    }
}