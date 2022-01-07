using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Settings;

namespace Smartstore.PayPal.Filters
{
    public class CheckoutFilter : IAsyncResultFilter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly PayPalSettings _settings;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Lazy<IWidgetProvider> _widgetProvider;

        public CheckoutFilter(
            SmartDbContext db,
            ICommonServices services, 
            PayPalSettings settings, 
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor,
            Lazy<IWidgetProvider> widgetProvider)
        {
            _db = db;
            _services = services;
            _settings = settings;
            _checkoutStateAccessor = checkoutStateAccessor;
            _httpContextAccessor = httpContextAccessor;
            _widgetProvider = widgetProvider;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            // If client id or secret haven't been configured yet, don't do anything.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                await next();
                return;
            }
            
            var controller = filterContext.RouteData.Values.GetControllerName().EmptyNull();
            var action = filterContext.RouteData.Values.GetActionName().EmptyNull();
            var isPaymentSelectionPage = controller == "Checkout" && action == "PaymentMethod";

            // Render on cart, payment selection, order confirmation or everywhere if mini cart setting is turned on.
            if (!isPaymentSelectionPage)
            {
                await next();
                return;
            }

            var checkoutState = _checkoutStateAccessor.CheckoutState;

            if(!checkoutState.CustomProperties.ContainsKey("PayPalButtonUsed"))
            {
                _widgetProvider.Value.RegisterWidget("checkout_payment_method_buttons", new ComponentWidgetInvoker(typeof(PayPalViewComponent)));
                await next();
                return;
            }

            var skipPaymentPage = (bool)checkoutState.CustomProperties.Get("PayPalButtonUsed");

            // Should only run on a full view rendering result or HTML ContentResult.
            if ((filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult()) && skipPaymentPage)
            {
                _services.WorkContext.CurrentCustomer.GenericAttributes.SelectedPaymentMethod = "Payments.PayPalStandard";
                await _db.SaveChangesAsync();

                var session = _httpContextAccessor.HttpContext.Session;

                if (!session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var processPaymentRequest))
                {
                    processPaymentRequest = new ProcessPaymentRequest
                    {
                        PaypalOrderId = (string)checkoutState.CustomProperties.Get("PayPalOrderId")
                    };
                    session.TrySetObject("OrderPaymentInfo", processPaymentRequest);
                };

                // Delete property for backward navigation.
                checkoutState.CustomProperties.Remove("PayPalButtonUsed");

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "Controller", "Checkout" },
                        { "Action", "Confirm" },
                        { "area", null }
                    });
            }

            await next();
        }
    }
}

