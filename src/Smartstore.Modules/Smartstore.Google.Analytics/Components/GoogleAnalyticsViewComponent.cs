using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Services;
using Smartstore.Http;
using Smartstore.Utilities;
using Smartstore.Web.Components;
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

            var rootScript = string.Empty;
            
            try
            {
                var routeData = HttpContext.GetRouteData();
                var controller = routeData.Values.GetControllerName();
                var action = routeData.Values.GetActionName();

                // None of the Google Tag Manager code should be rendered when old (unmigrated) tracking code is still used.
                var isOldScript = _settings.EcommerceScript.Contains("analytics.js");

                using var psb = StringBuilderPool.Instance.Get(out var sb);
                using var writer = new StringWriter(sb);

                if (!isOldScript)
                {
                    if (_settings.RenderCatalogScripts)
                    {
                        if (controller.EqualsNoCase("product") && action.EqualsNoCase("productdetails"))
                        {
                            // Product details page > view_item
                            await _googleAnalyticsScriptHelper.WriteViewItemScriptAsync(writer, sb, (ProductDetailsModel)model);
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
                                await _googleAnalyticsScriptHelper.WriteListScriptAsync(writer, sb, productList, action.ToLower(), catId);
                            }
                        }
                        else if (controller.EqualsNoCase("search") && action.EqualsNoCase("search"))
                        {
                            var searchModel = (SearchResultModel)model;
                            var productList = searchModel.TopProducts.Items;

                            _googleAnalyticsScriptHelper.WriteSearchTermScript(writer, searchModel.Term);
                            await _googleAnalyticsScriptHelper.WriteListScriptAsync(writer, sb, productList, action.ToLower());
                        }
                    }
                    if (_settings.RenderCheckoutScripts)
                    {
                        if (controller.EqualsNoCase("shoppingcart") && action.EqualsNoCase("cart"))
                        {
                            // Cart page > view_cart + remove_from_cart 
                            await _googleAnalyticsScriptHelper.WriteCartScriptAsync(writer, sb, (ShoppingCartModel)model);
                        }
                        else if (controller.EqualsNoCase("checkout"))
                        {
                            if (action.EqualsNoCase("billingaddress"))
                            {
                                // Select billing address > begin_checkout
                                await _googleAnalyticsScriptHelper.WriteCheckoutScriptAsync(writer, sb);
                            }
                            else if (action.EqualsNoCase("paymentmethod"))
                            {
                                // Payment method page > add_shipping_info
                                await _googleAnalyticsScriptHelper.WriteCheckoutScriptAsync(writer, sb, addShippingInfo: true);
                            }
                            else if (action.EqualsNoCase("confirm"))
                            {
                                // Confirm order page > add_payment_info
                                await _googleAnalyticsScriptHelper.WriteCheckoutScriptAsync(writer, sb, addPaymentInfo: true);
                            }
                        }
                    }
                }

                // Special case: must be rendered also for old script
                if (controller.EqualsNoCase("checkout") && action.EqualsNoCase("completed") && _settings.RenderCheckoutScripts)
                {
                    // Checkout completed page > purchase
                    await _googleAnalyticsScriptHelper.WriteOrderCompletedScriptAsync(writer, sb);
                }

                var cookiesAllowed = _cookieConsentManager.IsCookieAllowed(CookieType.Analytics);

                rootScript = _googleAnalyticsScriptHelper.GetTrackingScript(cookiesAllowed)
                    .Replace("{ECOMMERCE}", writer.ToString());
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