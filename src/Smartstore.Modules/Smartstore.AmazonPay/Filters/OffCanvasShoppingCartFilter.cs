using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.AmazonPay.Components;
using Smartstore.Core.Widgets;

namespace Smartstore.AmazonPay.Filters
{
    /// <summary>
    /// Amazon Pay button in off-canvas shopping cart.
    /// </summary>
    public class OffCanvasShoppingCartFilter : IResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly AmazonPaySettings _amazonPaySettings;

        public OffCanvasShoppingCartFilter(
            Lazy<IWidgetProvider> widgetProvider,
            AmazonPaySettings amazonPaySettings)
        {
            _widgetProvider = widgetProvider;
            _amazonPaySettings = amazonPaySettings;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (_amazonPaySettings.ShowButtonInMiniShoppingCart && context.Result.IsHtmlViewResult())
            {
                var widget = new ComponentWidgetInvoker(typeof(AmazonPayButtonViewComponent));
                _widgetProvider.Value.RegisterWidget(new[] { "offcanvas_cart_summary" }, widget);
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
