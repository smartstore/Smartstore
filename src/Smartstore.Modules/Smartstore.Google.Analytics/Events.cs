using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Html;
using Smartstore.Core.Widgets;
using Smartstore.Events;
using Smartstore.Google.Analytics.Services;
using Smartstore.Web.Components;

namespace Smartstore.Google.Analytics
{
    public class Events : IConsumer
    {
        private static readonly Dictionary<Type, string> _interceptableViewComponents = new()
        {
            { typeof(HomeProductsViewComponent), "home_page_after_products" },
            { typeof(HomeBestSellersViewComponent), "home_page_after_bestsellers" },
            { typeof(RecentlyViewedProductsViewComponent), "after_recently_viewed_products" },
            { typeof(CrossSellProductsViewComponent), "after_cross_sell_products" }
        };

        private readonly GoogleAnalyticsSettings _settings;
        private readonly GoogleAnalyticsScriptHelper _googleAnalyticsScriptHelper;
        private readonly IWidgetProvider _widgetProvider;

        public Events(GoogleAnalyticsSettings settings, GoogleAnalyticsScriptHelper googleAnalyticsScriptHelper, IWidgetProvider widgetProvider)
        {
            _settings = settings;
            _googleAnalyticsScriptHelper = googleAnalyticsScriptHelper;
            _widgetProvider = widgetProvider;
        }

        public async Task HandleEventAsync(ViewComponentResultExecutingEvent message)
        {
            // If GoogleId is empty or is default don't render anything. Also if catalog scripts are configured not to be rendered.
            if (!_settings.GoogleId.HasValue() || _settings.GoogleId == "UA-0000000-0" || !_settings.RenderCatalogScripts)
                return;

            var componentType = message.Descriptor.TypeInfo.AsType();

            if (!_interceptableViewComponents.TryGetValue(componentType, out var zone))
            {
                return;
            }
            else
            {
                var model = (ProductSummaryModel)message.Model;
                var productList = model.Items;
                var componentName = message.Descriptor.ShortName;

                if (productList.Count > 0)
                {
                    var itemsScript = await _googleAnalyticsScriptHelper.GetListScriptAsync(productList, componentName);

                    if (_settings.MinifyScripts)
                    {
                        itemsScript = new JsMinifier().Minify(itemsScript);
                    }

                    _widgetProvider.RegisterHtml(zone, new HtmlString($"<script>{itemsScript}</script>"));
                }
            }
        }
    }
}