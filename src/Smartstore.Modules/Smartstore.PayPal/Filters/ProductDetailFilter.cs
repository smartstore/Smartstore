using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Filters
{
    /// <summary>
    /// Registers the ViewComponent to render the pay later widget on the product detail page.
    /// </summary>
    public class ProductDetailFilter : IAsyncResultFilter
    {
        private readonly ICommonServices _services;
        private readonly IWidgetProvider _widgetProvider;
        private readonly PayPalSettings _settings;
        private readonly IPaymentService _paymentService;

        public ProductDetailFilter(ICommonServices services, IWidgetProvider widgetProvider, PayPalSettings settings, IPaymentService paymentService)
        {
            _services = services;
            _widgetProvider = widgetProvider;
            _settings = settings;
            _paymentService = paymentService;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (!await IsPayPalStandardActive())
            {
                await next();
                return;
            }

            if (_settings.DisplayProductDetailPayLaterWidget && context.Result.IsHtmlViewResult())
            {
                _widgetProvider.RegisterViewComponent<PayPalPayLaterViewComponent>("productdetail_action_links_after");
            }

            await next();
        }
        private Task<bool> IsPayPalStandardActive()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalStandard", null, _services.StoreContext.CurrentStore.Id);
    }
}