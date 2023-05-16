using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Services;

namespace Smartstore.PayPal.Filters
{
    public class OffCanvasShoppingCartFilter : IAsyncResultFilter
    {
        private readonly ICommonServices _services;
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly PayPalHelper _payPalHelper;

        public OffCanvasShoppingCartFilter(ICommonServices services, PayPalSettings settings, IWidgetProvider widgetProvider, PayPalHelper payPalHelper)
        {
            _services = services;
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
                if (fundings.Contains(((int)FundingOptions.paypal).ToString()) && await _payPalHelper.IsPayPalStandardActiveAsync())
                {
                    _widgetProvider.RegisterViewComponent<PayPalViewComponent>("offcanvas_cart_summary");
                }

                // SEPA
                if (fundings.Contains(((int)FundingOptions.sepa).ToString()) && await _payPalHelper.IsSepaActiveAsync())
                {
                    _widgetProvider.RegisterViewComponent<PayPalSepaViewComponent>("offcanvas_cart_summary");
                }

                // PayLater
                if (fundings.Contains(((int)FundingOptions.paylater).ToString()) && await _payPalHelper.IsPayLaterActiveAsync())
                {
                    _widgetProvider.RegisterViewComponent<PayPalPayLaterViewComponent>("offcanvas_cart_summary");
                }
            }

            await next();
        }
    }
}
