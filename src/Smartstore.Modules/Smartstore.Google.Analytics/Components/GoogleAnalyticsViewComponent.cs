using System.Text.RegularExpressions;
using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Services;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Web.Components;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Google.Analytics.Components
{
    // TODO: (mh) (core)
    // - For select_item we might need an AJAX call on success we fire gtag("event", "select_item", {...
    // - Get rid of all .Replace("{
    public class GoogleAnalyticsViewComponent : SmartViewComponent
    {
        private static readonly JsMinifier Minifier = new();

        private readonly GoogleAnalyticsSettings _settings;
        private readonly ICookieConsentManager _cookieConsentManager;
        private readonly GoogleAnalyticsScriptHelper _googleAnalyticsScriptHelper;
        
        public GoogleAnalyticsViewComponent(
            GoogleAnalyticsSettings settings,
            ICookieConsentManager cookieConsentManager,
            GoogleAnalyticsScriptHelper googleAnalyticsScriptHelper)
        {
            _settings = settings;
            _cookieConsentManager = cookieConsentManager;
            _googleAnalyticsScriptHelper = googleAnalyticsScriptHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync(object model) 
        { 
            // If GoogleId is empty or is default don't render anything.
            if (!_settings.GoogleId.HasValue() || _settings.GoogleId == "UA-0000000-0")
            {
                return Empty();
            }

            var cookiesAllowed = _cookieConsentManager.IsCookieAllowed(CookieType.Analytics);
            var globalScript = _googleAnalyticsScriptHelper.GetTrackingScript(cookiesAllowed);
            var routeData = HttpContext.GetRouteData();

            try
            {
                var controller = routeData.Values.GetControllerName();
                var action = routeData.Values.GetActionName();

                if (controller.EqualsNoCase("product") && action.EqualsNoCase("productdetails") && _settings.RenderCatalogScripts)
                {
                    // TODO: (mh) (core) Test product detail output.
                    // Product details page
                    var viewItemScript = await _googleAnalyticsScriptHelper.GetViewItemScriptAsync((ProductDetailsModel)model);
                    globalScript = globalScript.Replace("{ECOMMERCE}", viewItemScript);
                }
                else if (controller.EqualsNoCase("catalog") && _settings.RenderCatalogScripts)
                {
                    // TODO: (mh) (core) Make plugin setting to define whether this should be rendered???
                    var catId = 0;
                    var productList = new List<ProductSummaryModel.SummaryItem>();

                    // Category, Manufacturer, RecentlyViewedProducts, RecentlyAddedProducts & CompareProducts pages
                    if (action.EqualsNoCase("category"))
                    {
                        var categoryModel = (CategoryModel)model;
                        productList = categoryModel.Products.Items;
                        catId = categoryModel.Id;
                    }
                    else if (action.EqualsNoCase("manufacturer"))
                    {
                        productList = ((BrandModel)model).Products.Items;
                    }
                    else if (action.EqualsNoCase("recentlyviewedproducts") ||
                             action.EqualsNoCase("recentlyaddedproducts") ||
                             action.EqualsNoCase("compareproducts"))
                    {
                        productList = ((ProductSummaryModel)model).Items;
                    }

                    // If there are no products in the list return just global script.
                    if (productList.Count > 0)
                    {
                        // TODO: (mh) (core) Maybe pass another list id & list name
                        var itemsScript = await _googleAnalyticsScriptHelper.GetListScriptAsync(productList, action.ToLower(), catId);
                        globalScript = globalScript.Replace("{ECOMMERCE}", itemsScript);
                    }
                    else
                    {
                        globalScript = globalScript.Replace("{ECOMMERCE}", string.Empty);
                    }
                }
                else if (controller.EqualsNoCase("checkout") && action.EqualsNoCase("completed"))
                {
                    // Checkout completed
                    var orderCompletedScript = await _googleAnalyticsScriptHelper.GetOrderCompletedScriptAsync();
                    globalScript = globalScript.Replace("{ECOMMERCE}", orderCompletedScript);
                }
                else
                {
                    // Global
                    globalScript = globalScript.Replace("{ECOMMERCE}", string.Empty);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error creating scripts for google ecommerce tracking");
            }

            if (_settings.MinifyScripts)
            {
                globalScript = Minifier.Minify(globalScript);
            }
            
            return HtmlContent(globalScript);
        }
    }
}