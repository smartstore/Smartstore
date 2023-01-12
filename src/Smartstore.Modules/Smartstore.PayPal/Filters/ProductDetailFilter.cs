using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Filters
{
    /// <summary>
    /// Registers the ViewComponent to render the pay later widget on the product detail page.
    /// </summary>
    public class ProductDetailFilter : IAsyncResultFilter
    {
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly PayPalHelper _payPalHelper;

        public ProductDetailFilter(PayPalSettings settings, IWidgetProvider widgetProvider, PayPalHelper payPalHelper)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _payPalHelper = payPalHelper;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (!await _payPalHelper.IsPaymentMethodActiveAsync("Payments.PayPalStandard"))
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
    }
}