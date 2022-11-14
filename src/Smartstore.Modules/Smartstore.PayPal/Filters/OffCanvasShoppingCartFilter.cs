using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Filters
{
    public class OffCanvasShoppingCartFilter : IAsyncResultFilter
    {
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;

        public OffCanvasShoppingCartFilter(ICommonServices services, IPaymentService paymentService, PayPalSettings settings, IWidgetProvider widgetProvider)
        {
            _services = services;
            _paymentService = paymentService;
            _settings = settings;
            _widgetProvider = widgetProvider;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            if (!await IsPayPalStandardActive())
            {
                await next();
                return;
            }
            
            // If client id or secret haven't been configured yet, don't show button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                await next();
                return;
            }

            if (!_settings.ShowButtonInMiniShoppingCart)
            {
                await next();
                return;
            }

            // Should only run on a full view rendering result or HTML ContentResult.
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                _widgetProvider.RegisterViewComponent<PayPalViewComponent>("offcanvas_cart_summary");
            }

            await next();
        }

        private Task<bool> IsPayPalStandardActive()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalStandard", null, _services.StoreContext.CurrentStore.Id);
    }
}
