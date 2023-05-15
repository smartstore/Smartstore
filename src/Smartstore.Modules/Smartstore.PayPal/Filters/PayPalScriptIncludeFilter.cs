using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;
using Smartstore.PayPal.Services;
using Smartstore.Utilities;

namespace Smartstore.PayPal.Filters
{
    /// <summary>
    /// Renders a script to detect buyer fraud early by collecting the buyer's browser information during checkout and passing it to PayPal (must be active for pay per invoice). 
    /// Also renders the PayPal JS SDK standard script & PayPal helper script which contains function to initialize Buttons, Hosted Fields and APMs (alternative payment methods).
    /// </summary>
    public class PayPalScriptIncludeFilter : IAsyncActionFilter
    {
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ICommonServices _services;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly PayPalHelper _payPalHelper;
        private readonly PayPalHttpClient _client;

        public PayPalScriptIncludeFilter(
            PayPalSettings settings, 
            IWidgetProvider widgetProvider,
            ICommonServices services,
            ICheckoutStateAccessor checkoutStateAccessor,
            PayPalHelper payPalHelper,
            PayPalHttpClient client)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _services = services;
            _checkoutStateAccessor = checkoutStateAccessor;
            _payPalHelper = payPalHelper;
            _client = client;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (await _payPalHelper.IsAnyMethodActiveAsync(
                "Payments.PayPalStandard",
                "Payments.PayPalCreditCard",
                "Payments.PayPalPayLater",
                "Payments.PayPalSepa"))
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
                    $"&integration-date=2021-04-13" +
                    // TODO: (mh) (core) Must not be set for APMs but for paypal, paylater & sepa if intent is set to authorize.
                    //$"&commit=false" +
                    $"&components=messages,buttons,hosted-fields,funding-eligibility";

                scriptUrl += $"&enable-funding=" + await GetFundingOptionsAsync();

                scriptUrl += $"&intent={_settings.Intent.ToString().ToLower()}";
                scriptUrl += $"&locale={_services.WorkContext.WorkingLanguage.LanguageCulture.Replace("-", "_")}";

                var clientToken = await _payPalHelper.IsCreditCardActiveAsync() 
                    ? await GetClientToken(context.HttpContext) 
                    : string.Empty;

                _widgetProvider.RegisterHtml("end", new HtmlString($"<script src='{scriptUrl}' data-partner-attribution-id='SmartStore_Cart_PPCP' data-client-token='{clientToken}' async id='paypal-js'></script>"));
                _widgetProvider.RegisterHtml("end", new HtmlString($"<script src='/Modules/Smartstore.PayPal/js/paypal.utils.js'></script>"));
            }

            if (!await _payPalHelper.IsPayUponInvoiceActiveAsync())
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

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            sb.Append("<script type='application/json' fncls='fnparams-dede7cc5-15fd-4c75-a9f4-36c430ee3a99'>");
            // INFO: Single quotes (') aren't allowed to delimit strings.
            sb.Append("{\"sandbox\":" + (_settings.UseSandbox ? "true" : "false") + ",\"f\":\"" + clientMetaId + "\",\"s\":\"" + sourceIdentifier + "\" }");
            sb.Append("</script>");
            sb.Append("<script type='text/javascript' src='https://c.paypal.com/da/r/fb.js'></script>");
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
        /// Gets active funding sources by checking active providers combined with default fundings.
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetFundingOptionsAsync()
        {
            // Set default fundings which are always available
            var result = "sepa,paylater";

            if (await _payPalHelper.IsCreditCardActiveAsync())
            {
                result += ",card";
            }

            return result;
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
            if (clientToken.HasValue())
            {
                return clientToken;
            }

            // Get client token from PayPal REST API.
            var response = await _client.ExecuteRequestAsync(new GenerateClientTokenRequest());
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            clientToken = (string)jResponse.client_token;

            session.SetString("PayPalClientToken", clientToken);

            return clientToken;
        }
    }
}