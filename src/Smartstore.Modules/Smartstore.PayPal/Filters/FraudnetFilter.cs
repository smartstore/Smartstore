using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;
using Smartstore.Utilities;

namespace Smartstore.PayPal.Filters
{
    /// <summary>
    /// Renders a script to detect buyer fraud early by collecting the buyer's browser information during checkout and passing it to PayPal.
    /// Must be active for pay per invoice. 
    /// </summary>
    public class FraudnetFilter : IAsyncActionFilter
    {
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public FraudnetFilter(
            PayPalSettings settings, 
            IWidgetProvider widgetProvider,
            ICommonServices services,
            IPaymentService paymentService,
            ICheckoutStateAccessor checkoutStateAccessor)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _services = services;
            _paymentService = paymentService;
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // If client id or secret haven't been configured yet, don't show button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                await next();
                return;
            }

            if (!await IsPayUponInvoiceActive())
            {
                await next();
                return;
            }

            // Write fraudnet script include.
            var routeData = context.RouteData;
            var routeId = routeData.Values.GenerateRouteIdentifier();

            // Risk Session Correlation ID / Client Metadata ID has to be uniqueand invariant to the current checkout.
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
            sb.Append("{\"sandbox\":true,\"f\":\"" + clientMetaId + "\",\"s\":\"" + sourceIdentifier + "\" }");
            sb.Append("</script>");
            sb.Append("<script type='text/javascript' src='https://c.paypal.com/da/r/fb.js'></script>");
            sb.Append($"<noscript><img src='https://c.paypal.com/v1/r/d/b/ns?f={clientMetaId}&s={sourceIdentifier}&js=0&r=1' /></noscript>");

            _widgetProvider.RegisterHtml("end", new HtmlString(sb.ToString()));

            await next();
        }

        private Task<bool> IsPayUponInvoiceActive()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalPayUponInvoice", null, _services.StoreContext.CurrentStore.Id);

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
    }
}