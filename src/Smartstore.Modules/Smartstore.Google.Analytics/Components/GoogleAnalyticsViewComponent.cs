using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Services;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Http;
using Smartstore.Web.Components;
using Smartstore.Web.Models.Cart;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Search;

namespace Smartstore.Google.Analytics.Components
{
    // TODO: (mh) (core)
    // - Get rid of all .Replace("{
    // - Should we also consider offcanvas cart? May this would be zu viel des guten :-)
    // - Maybe pass another list id & list name to GetListScriptAsync(...)
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

            var globalScript = string.Empty;
            var specificScript = string.Empty;
            var includeScriptFile = false;

            try
            {
                var routeData = HttpContext.GetRouteData();
                var controller = routeData.Values.GetControllerName();
                var action = routeData.Values.GetActionName();

                // None of the Google Tag Manager code should be rendered when old (unmigrated) tracking code is still used.
                var isOldScript = _settings.EcommerceScript.Contains("analytics.js");

                var cookiesAllowed = _cookieConsentManager.IsCookieAllowed(CookieType.Analytics);
                globalScript += _googleAnalyticsScriptHelper.GetTrackingScript(cookiesAllowed);

                if (_settings.RenderCatalogScripts && !isOldScript)
                {
                    if (controller.EqualsNoCase("product") && action.EqualsNoCase("productdetails"))
                    {
                        // Product details page > view_item
                        specificScript = await _googleAnalyticsScriptHelper.GetViewItemScriptAsync((ProductDetailsModel)model);
                    }
                    else if (controller.EqualsNoCase("catalog"))
                    {
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
                            includeScriptFile = true;
                            specificScript = await _googleAnalyticsScriptHelper.GetListScriptAsync(productList, action.ToLower(), catId);
                        }
                    }
                    else if (controller.EqualsNoCase("search") && action.EqualsNoCase("search"))
                    {
                        includeScriptFile = true;

                        var searchModel = (SearchResultModel)model;
                        var productList = searchModel.TopProducts.Items;

                        specificScript = _googleAnalyticsScriptHelper.GetSearchTermScript(searchModel.Term);
                        specificScript += await _googleAnalyticsScriptHelper.GetListScriptAsync(productList, action.ToLower());
                    }
                }
                else if (_settings.RenderCheckoutScripts && !isOldScript)
                {
                    if (controller.EqualsNoCase("shoppingcart") && action.EqualsNoCase("cart"))
                    {
                        includeScriptFile = true;

                        // Cart page > view_cart + remove_from_cart 
                        specificScript = await _googleAnalyticsScriptHelper.GetCartScriptAsync((ShoppingCartModel)model);
                    }
                    else if (controller.EqualsNoCase("checkout"))
                    {
                        if (action.EqualsNoCase("billingaddress"))
                        {
                            // Select billing address > begin_checkout
                            specificScript = await _googleAnalyticsScriptHelper.GetCheckoutScriptAsync();
                        }
                        else if (action.EqualsNoCase("paymentmethod"))
                        {
                            // Payment method page > add_shipping_info
                            specificScript = await _googleAnalyticsScriptHelper.GetCheckoutScriptAsync(addShippingInfo: true);
                        }
                        else if (action.EqualsNoCase("confirm"))
                        {
                            // Confirm order page > add_payment_info
                            specificScript = await _googleAnalyticsScriptHelper.GetCheckoutScriptAsync(addPaymentInfo: true);
                        }
                    }
                }
                else if (controller.EqualsNoCase("checkout") && action.EqualsNoCase("completed"))
                {
                    // Checkout completed page > purchase
                    specificScript = await _googleAnalyticsScriptHelper.GetOrderCompletedScriptAsync();
                }

                globalScript = globalScript.Replace("{ECOMMERCE}", specificScript);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error creating scripts for google ecommerce tracking");
            }

            if (_settings.MinifyScripts && globalScript.HasValue())
            {
                globalScript = Minifier.Minify(globalScript);
            }

            // Include script only when product lists or cart will be rendered
            if (includeScriptFile)
            {
                var path = WebHelper.ToAbsolutePath("~/Modules/Smartstore.Google.Analytics/js/google-analytics.utils.js");
                globalScript = $"<script src='{path}'></script>\n{globalScript}";
            }

            return HtmlContent(globalScript);
        }
    }
}