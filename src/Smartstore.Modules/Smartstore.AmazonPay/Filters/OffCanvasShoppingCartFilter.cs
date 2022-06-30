using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.AmazonPay.Components;
using Smartstore.Core.Widgets;

namespace Smartstore.AmazonPay.Filters
{
    /// <summary>
    /// AmazonPay button in off-canvas shopping cart.
    /// </summary>
    public class OffCanvasShoppingCartFilter : IResultFilter
    {
        private readonly IWidgetProvider _widgetProvider;
        private readonly AmazonPaySettings _amazonPaySettings;

        public OffCanvasShoppingCartFilter(IWidgetProvider widgetProvider, AmazonPaySettings amazonPaySettings)
        {
            _widgetProvider = widgetProvider;
            _amazonPaySettings = amazonPaySettings;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (_amazonPaySettings.ShowButtonInMiniShoppingCart && context.Result.IsHtmlViewResult())
            {
                _widgetProvider.RegisterViewComponent<PayButtonViewComponent>("offcanvas_cart_summary");
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
