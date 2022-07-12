using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Services;
using Smartstore.Google.Analytics.Settings;
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
    // - Make sure none of the new code will be rendered when old (unmigrated) js-code is still used.
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

            try
            {
                var routeData = HttpContext.GetRouteData();
                var controller = routeData.Values.GetControllerName();
                var action = routeData.Values.GetActionName();
                
                var cookiesAllowed = _cookieConsentManager.IsCookieAllowed(CookieType.Analytics);
                globalScript = _googleAnalyticsScriptHelper.GetTrackingScript(cookiesAllowed);

                if (_settings.RenderCatalogScripts)
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
                            specificScript = await _googleAnalyticsScriptHelper.GetListScriptAsync(productList, action.ToLower(), catId);
                        }
                    }
                    else if (controller.EqualsNoCase("search") && action.EqualsNoCase("search"))
                    {
                        var searchModel = (SearchResultModel)model;
                        var productList = searchModel.TopProducts.Items;

                        specificScript = _googleAnalyticsScriptHelper.GetSearchTermScript(searchModel.Term);
                        specificScript += await _googleAnalyticsScriptHelper.GetListScriptAsync(productList, action.ToLower());
                    }
                }
                else if (_settings.RenderCheckoutScripts)
                {
                    if (controller.EqualsNoCase("shoppingcart") && action.EqualsNoCase("cart"))
                    {
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
                        else if (action.EqualsNoCase("completed"))
                        {
                            // Checkout completed page > purchase
                            specificScript = await _googleAnalyticsScriptHelper.GetOrderCompletedScriptAsync();
                        }
                    }
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
            
            return HtmlContent(globalScript);
        }
    }
}