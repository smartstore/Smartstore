using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Services;
using Smartstore.Web.Components;
using Smartstore.Web.Models.Search;

namespace Smartstore.Google.Analytics.Components
{
    public class GoogleAnalyticsViewComponent : SmartViewComponent
    {
        private static readonly JsMinifier Minifier = new();

        private readonly GoogleAnalyticsSettings _settings;
        private readonly ICookieConsentManager _cookieConsentManager;
        private readonly GoogleAnalyticsScriptHelper _googleAnalyticsScriptHelper;
        private readonly OrderSettings _orderSettings;

        public GoogleAnalyticsViewComponent(
            GoogleAnalyticsSettings settings,
            ICookieConsentManager cookieConsentManager,
            GoogleAnalyticsScriptHelper googleAnalyticsScriptHelper,
            OrderSettings orderSettings)
        {
            _settings = settings;
            _cookieConsentManager = cookieConsentManager;
            _googleAnalyticsScriptHelper = googleAnalyticsScriptHelper;
            _orderSettings = orderSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(object model)
        {
            // If GoogleId is empty or is default don't render anything.
            if (!_settings.GoogleId.HasValue() || _settings.GoogleId == "UA-0000000-0")
            {
                return Empty();
            }

            // If user has not accepted the cookie consent don't render anything.
            if (_settings.RenderWithUserConsentOnly && !await _cookieConsentManager.IsCookieAllowedAsync(CookieType.Analytics))
            {
                return Empty();
            }

            var rootScript = string.Empty;
            var specificScript = string.Empty;

            try
            {
                var routeData = HttpContext.GetRouteData();
                var controller = routeData.Values.GetControllerName();
                var action = routeData.Values.GetActionName();

                // None of the Google Tag Manager code should be rendered when old (unmigrated) tracking code is still used.
                var isOldScript = _settings.EcommerceScript.Contains("analytics.js");

                if (!isOldScript)
                {
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
                            var productList = new List<ProductSummaryItemModel>();

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
                    if (_settings.RenderCheckoutScripts)
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
                        }
                    }
                }

                // Special case: must be rendered also for old script
                if (controller.EqualsNoCase("checkout") && action.EqualsNoCase("completed") && _settings.RenderCheckoutScripts)
                {
                    // Checkout completed page > purchase
                    specificScript = await _googleAnalyticsScriptHelper.GetOrderCompletedScriptAsync();
                }
                else if (controller.EqualsNoCase("order") && action.EqualsNoCase("details") && _orderSettings.DisableOrderCompletedPage)
                {
                    // If order was just being completed render script.
                    var orderCompleted = HttpContext.Session.GetString("GA-OrderCompleted").ToBool();
                    if (orderCompleted)
                    {
                        // Order details page
                        specificScript = await _googleAnalyticsScriptHelper.GetOrderCompletedScriptAsync();
                    }
                }

                var cookiesAllowed = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.Analytics);
                var adUserDataAllowed = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.ConsentAdUserData);
                var adPersonalizationAllowed = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.ConsentAdPersonalization);

                rootScript = _googleAnalyticsScriptHelper.GetTrackingScript(cookiesAllowed, adUserDataAllowed, adPersonalizationAllowed)
                    .Replace("{ECOMMERCE}", specificScript);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error creating scripts for google ecommerce tracking");
            }

            if (_settings.MinifyScripts && rootScript.HasValue())
            {
                rootScript = Minifier.Minify(rootScript);
            }

            var path = Url.Content("~/Modules/Smartstore.Google.Analytics/js/google-analytics.utils.js");
            rootScript = $"<script src='{path}'></script>\n{rootScript}";

            return HtmlContent(rootScript);
        }
    }
}