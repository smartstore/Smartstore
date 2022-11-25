using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;
using Smartstore.Web.Models.Checkout;

namespace Smartstore.PayPal.Filters
{
    public class CheckoutFilter : IAsyncResultFilter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;
        private readonly PayPalSettings _settings;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Lazy<IWidgetProvider> _widgetProvider;

        public CheckoutFilter(
            SmartDbContext db,
            ICommonServices services,
            IPaymentService paymentService,
            PayPalSettings settings,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor,
            Lazy<IWidgetProvider> widgetProvider)
        {
            _db = db;
            _services = services;
            _paymentService = paymentService;
            _settings = settings;
            _checkoutStateAccessor = checkoutStateAccessor;
            _httpContextAccessor = httpContextAccessor;
            _widgetProvider = widgetProvider;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            if (!await IsPayPalStandardActive())
            {
                await next();
                return;
            }

            // If client id or secret haven't been configured yet, don't do anything.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                await next();
                return;
            }

            var checkoutState = _checkoutStateAccessor.CheckoutState;

            if (!checkoutState.CustomProperties.ContainsKey("PayPalButtonUsed"))
            {
                if (filterContext.Result is not ViewResult viewResult || viewResult.Model is not CheckoutPaymentMethodModel model)
                {
                    await next();
                    return;
                }

                var isSelected = false;
                var firstPaymentMethod = model.PaymentMethods.First(); 
                if (firstPaymentMethod != null)
                {
                    isSelected = firstPaymentMethod.PaymentMethodSystemName == "Payments.PayPalStandard" && firstPaymentMethod.Selected;
                }

                _widgetProvider.Value.RegisterViewComponent<PayPalViewComponent>("checkout_payment_method_buttons", new { isPaymentInfoInvoker = false, isSelected });

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

                if (!session.ContainsKey("OrderPaymentInfo"))
                {
                    session.TrySetObject("OrderPaymentInfo", new ProcessPaymentRequest
                    {
                        PayPalOrderId = (string)checkoutState.CustomProperties.Get("PayPalOrderId"),
                        StoreId = _services.StoreContext.CurrentStore.Id,
                        CustomerId = _services.WorkContext.CurrentCustomer.Id,
                        PaymentMethodSystemName = "Payments.PayPalStandard"
                    });
                };

                // Delete property for backward navigation.
                checkoutState.CustomProperties.Remove("PayPalButtonUsed");

                filterContext.Result = new RedirectToActionResult("Confirm", "Checkout", new { area = "" });
            }

            await next();
        }

        private Task<bool> IsPayPalStandardActive()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalStandard", null, _services.StoreContext.CurrentStore.Id);
    }
}