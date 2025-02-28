using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Filters
{
    public class OffCanvasShoppingCartFilter : IAsyncResultFilter
    {
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly PayPalHelper _payPalHelper;

        public OffCanvasShoppingCartFilter(PayPalSettings settings, IWidgetProvider widgetProvider, PayPalHelper payPalHelper)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _payPalHelper = payPalHelper;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            // If client id or secret haven't been configured yet, don't show button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                await next();
                return;
            }
            
            // Should only run on a full view rendering result or HTML ContentResult.
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                var fundings = _settings.FundingsOffCanvasCart;

                // PayPalStandard
                if (fundings.Contains(FundingOptions.paypal.ToString()) && await _payPalHelper.IsProviderActiveAsync(PayPalConstants.Standard))
                {
                    _widgetProvider.RegisterViewComponent<PayPalViewComponent>("offcanvas_cart_summary");
                }

                // SEPA
                if (fundings.Contains(FundingOptions.sepa.ToString()) && await _payPalHelper.IsProviderActiveAsync(PayPalConstants.Sepa))
                {
                    _widgetProvider.RegisterViewComponent<PayPalSepaViewComponent>("offcanvas_cart_summary");
                }

                // PayLater
                if (fundings.Contains(FundingOptions.paylater.ToString()) && await _payPalHelper.IsProviderActiveAsync(PayPalConstants.PayLater))
                {
                    _widgetProvider.RegisterViewComponent<PayPalPayLaterViewComponent>("offcanvas_cart_summary");
                }

                // GooglePay
                if (fundings.Contains(FundingOptions.googlepay.ToString()) && await _payPalHelper.IsProviderActiveAsync(PayPalConstants.GooglePay))
                {
                    _widgetProvider.RegisterViewComponent<PayPalGooglePayViewComponent>("offcanvas_cart_summary");
                }
            }

            await next();
        }
    }
}