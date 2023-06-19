using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.StripeElements.Components;
using Smartstore.StripeElements.Services;
using Smartstore.StripeElements.Settings;

namespace Smartstore.StripeElements.Filters
{
    public class OffCanvasShoppingCartFilter : IAsyncResultFilter
    {
        private readonly StripeSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly StripeHelper _stripeHelper;

        public OffCanvasShoppingCartFilter(StripeSettings settings,  IWidgetProvider widgetProvider, StripeHelper stripeHelper)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _stripeHelper = stripeHelper;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
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

            if (!await _stripeHelper.IsStripeElementsActive())
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
    }
}
