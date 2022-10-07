using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Filters
{
    /// <summary>
    /// Registers the ViewComponent to render the pay later widget on the product detail page.
    /// </summary>
    public class ProductDetailFilter : IResultFilter
    {
        private readonly IWidgetProvider _widgetProvider;
        private readonly PayPalSettings _settings;

        public ProductDetailFilter(IWidgetProvider widgetProvider, PayPalSettings settings)
        {
            _widgetProvider = widgetProvider;
            _settings = settings;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (_settings.DisplayProductDetailPayLaterWidget && context.Result.IsHtmlViewResult())
            {
                _widgetProvider.RegisterViewComponent<PayPalPayLaterViewComponent>("productdetail_action_links_after");
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}