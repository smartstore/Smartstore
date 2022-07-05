using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Utilities;
using Smartstore.Web.Components;

namespace Smartstore.Google.Analytics.Components
{
    public class GoogleAnalyticsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly GoogleAnalyticsSettings _settings;
        private readonly ICategoryService _categoryService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICookieConsentManager _cookieConsentManager;

        public GoogleAnalyticsViewComponent(
            SmartDbContext db,
            GoogleAnalyticsSettings settings,
            ICategoryService categoryService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICookieConsentManager cookieConsentManager)
        {
            _db = db;
            _settings = settings;
            _categoryService = categoryService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _cookieConsentManager = cookieConsentManager;
        }

        public async Task<IViewComponentResult> InvokeAsync() 
        { 
            // If GoogleId is empty or is default don't render anything.
            if (!_settings.GoogleId.HasValue() || _settings.GoogleId == "UA-0000000-0")
            {
                return Empty();
            }

            var globalScript = string.Empty;
            var routeData = HttpContext.GetRouteData();

            try
            {
                var cookiesAllowed = _cookieConsentManager.IsCookieAllowed(CookieType.Analytics);

                // Special case, if we are in last step of checkout, we can use order total for conversion value
                var controller = routeData.Values.GetControllerName();
                var action = routeData.Values.GetActionName();

                if (controller.EqualsNoCase("checkout") && action.EqualsNoCase("completed"))
                {
                    var lastOrder = await GetLastOrderAsync();
                    globalScript += await GetEcommerceScriptAsync(lastOrder, cookiesAllowed);
                }
                else
                {
                    globalScript += GetTrackingScript(cookiesAllowed);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error creating scripts for google ecommerce tracking");
            }

            return HtmlContent(globalScript);
        }

        private async Task<Order> GetLastOrderAsync()
        {
            var order = await _db.Orders
                .ApplyStandardFilter(Services.WorkContext.CurrentCustomer.Id, Services.StoreContext.CurrentStore.Id)
                .FirstOrDefaultAsync();

            return order;
        }

        private string GetOptOutCookieScript()
        {
            var script = @"
				var gaProperty = '{GOOGLEID}'; 
				var disableStr = 'ga-disable-' + gaProperty; 
				if (document.cookie.indexOf(disableStr + '=true') > -1) { 
					window[disableStr] = true;
				} 
				function gaOptout() { 
					document.cookie = disableStr + '=true; expires=Thu, 31 Dec 2099 23:59:59 UTC; path=/'; 
					window[disableStr] = true; 
					alert('{NOTIFICATION}'); 
				} 
			";

            script += "\n";
            script = script.Replace("{GOOGLEID}", _settings.GoogleId);
            script = script.Replace("{NOTIFICATION}", T("Plugins.Widgets.GoogleAnalytics.OptOutNotification").JsValue.ToString());

            return script;
        }

        private string GetTrackingScript(bool cookiesAllowed)
        {
            var script = _settings.TrackingScript + "\n";
            script = script.Replace("{GOOGLEID}", _settings.GoogleId);
            script = script.Replace("{ECOMMERCE}", "");
            script = script.Replace("{OPTOUTCOOKIE}", GetOptOutCookieScript());
            script = script.Replace("{STORAGETYPE}", cookiesAllowed ? "granted" : "denied");
            script = script.Replace("{USERID}", Services.WorkContext.CurrentCustomer.CustomerGuid.ToString());
            
            return script;
        }

        private async Task<string> GetEcommerceScriptAsync(Order order, bool cookiesAllowed)
        {
            var usCulture = new CultureInfo("en-US");

            var script = _settings.TrackingScript + "\n";
            script = script.Replace("{GOOGLEID}", _settings.GoogleId);
            script = script.Replace("{OPTOUTCOOKIE}", GetOptOutCookieScript());

            if (order != null)
            {
                var site = Services.StoreContext.CurrentStore.Url
                    .EmptyNull()
                    .Replace("http://", string.Empty)
                    .Replace("https://", string.Empty)
                    .Replace("/", string.Empty);

                var ecScript = _settings.EcommerceScript + "\n";
                ecScript = ecScript.Replace("{GOOGLEID}", _settings.GoogleId);
                ecScript = ecScript.Replace("{ORDERID}", order.GetOrderNumber());
                ecScript = ecScript.Replace("{SITE}", FixIllegalJavaScriptChars(site));
                ecScript = ecScript.Replace("{TOTAL}", order.OrderTotal.ToString("0.00", usCulture));
                ecScript = ecScript.Replace("{TAX}", order.OrderTax.ToString("0.00", usCulture));
                ecScript = ecScript.Replace("{SHIP}", order.OrderShippingInclTax.ToString("0.00", usCulture));
                ecScript = ecScript.Replace("{CITY}", order.BillingAddress == null ? string.Empty : FixIllegalJavaScriptChars(order.BillingAddress.City));
                ecScript = ecScript.Replace("{STATEPROVINCE}", order.BillingAddress == null || order.BillingAddress.StateProvince == null
                    ? string.Empty
                    : FixIllegalJavaScriptChars(order.BillingAddress.StateProvince.Name));
                ecScript = ecScript.Replace("{COUNTRY}", order.BillingAddress == null || order.BillingAddress.Country == null
                    ? string.Empty
                    : FixIllegalJavaScriptChars(order.BillingAddress.Country.Name));
                ecScript = ecScript.Replace("{CURRENCY}", order.CustomerCurrencyCode);

                using var psb = StringBuilderPool.Instance.Get(out var sb);
                using var writer = new StringWriter(sb);
                if (_settings.EcommerceDetailScript.HasValue())
                {
                    foreach (var item in order.OrderItems)
                    {
                        var ecDetailScript = _settings.EcommerceDetailScript;
                        var defaultProductCategory = (await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { item.ProductId })).FirstOrDefault();
                        var categoryName = defaultProductCategory != null ? defaultProductCategory.Category.Name : string.Empty;

                        // The SKU code is a required parameter for every item that is added to the transaction.
                        var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(item.ProductId, item.AttributeSelection);
                        if (attributeCombination != null)
                        {
                            item.Product.MergeWithCombination(attributeCombination);
                        }
                        
                        ecDetailScript = ecDetailScript.Replace("{ORDERID}", order.GetOrderNumber());
                        ecDetailScript = ecDetailScript.Replace("{PRODUCTSKU}", FixIllegalJavaScriptChars(item.Product.Sku));
                        ecDetailScript = ecDetailScript.Replace("{PRODUCTNAME}", FixIllegalJavaScriptChars(item.Product.Name));
                        ecDetailScript = ecDetailScript.Replace("{CATEGORYNAME}", FixIllegalJavaScriptChars(categoryName));
                        ecDetailScript = ecDetailScript.Replace("{UNITPRICE}", item.UnitPriceInclTax.ToString("0.00", usCulture));
                        ecDetailScript = ecDetailScript.Replace("{QUANTITY}", item.Quantity.ToString());

                        sb.AppendLine(ecDetailScript);
                    }
                }

                ecScript = ecScript.Replace("{DETAILS}", sb.ToString());
                script = script.Replace("{ECOMMERCE}", ecScript);

                // If no consent to third party cookies was given, set storage to none.
                script = script.Replace("{STORAGETYPE}", cookiesAllowed ? "'granted'" : "'denied'");
                script = script.Replace("{USERID}", Services.WorkContext.CurrentCustomer.CustomerGuid.ToString());
            }

            return script;
        }

        private static string FixIllegalJavaScriptChars(string text)
        {
            //replace ' with \' (http://stackoverflow.com/questions/4292761/need-to-url-encode-labels-when-tracking-events-with-google-analytics)
            return text?.Replace("'", "\\'") ?? string.Empty;
        }
    }
}
