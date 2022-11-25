using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.StripeElements.Components;
using Smartstore.StripeElements.Settings;
using Smartstore.StripeElements.Providers;

namespace Smartstore.StripeElements.Filters
{
    public class OffCanvasShoppingCartFilter : IAsyncResultFilter
    {
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;
        private readonly StripeSettings _settings;
        private readonly IWidgetProvider _widgetProvider;

        public OffCanvasShoppingCartFilter(ICommonServices services, IPaymentService paymentService, StripeSettings settings, IWidgetProvider widgetProvider)
        {
            _services = services;
            _paymentService = paymentService;
            _settings = settings;
            _widgetProvider = widgetProvider;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            if (!await IsStripeElementsActive())
            {
                await next();
                return;
            }

            // If api key hasn't been configured yet, don't show button.
            if (!_settings.PublicApiKey.HasValue() || !_settings.SecrectApiKey.HasValue())
            {
                await next();
                return;
            }

            if (!_settings.ShowButtonInMiniShoppingCart)
            {
                await next();
                return;
            }

            // should only run on a full view rendering result or HTML ContentResult
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                _widgetProvider.RegisterViewComponent<StripeElementsViewComponent>("offcanvas_cart_summary");
            }

            await next();
        }

        private Task<bool> IsStripeElementsActive()
            => _paymentService.IsPaymentMethodActiveAsync(StripeElementsProvider.SystemName, null, _services.StoreContext.CurrentStore.Id);
    }
}
