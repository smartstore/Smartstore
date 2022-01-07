using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;
using Smartstore.PayPal.Settings;

namespace Smartstore.PayPal.Filters
{
    public class MiniBasketFilter : IResultFilter
    {
        private readonly PayPalSettings _settings;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        
        public MiniBasketFilter(PayPalSettings settings, Lazy<IWidgetProvider> widgetProvider)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // If client id or secret haven't been configured yet, don't show button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
                return;

            if (!_settings.ShowButtonInMiniShoppingCart)
                return;

            // should only run on a full view rendering result or HTML ContentResult
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                _widgetProvider.Value.RegisterWidget("offcanvas_cart_summary", new ComponentWidgetInvoker(typeof(PayPalViewComponent)));
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
