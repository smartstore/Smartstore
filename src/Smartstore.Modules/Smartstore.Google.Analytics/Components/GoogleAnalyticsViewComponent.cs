using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Utilities;
using Smartstore.Web.Components;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Google.Analytics.Components
{
    // TODO: (mh) (core)
    // - How to intercept view components HomeBestSellers, HomeProducts, RecentlyViewedProducts 
    // - For select_item we might need an AJAX call on success we fire gtag("event", "select_item", {...

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

        public async Task<IViewComponentResult> InvokeAsync(object model) 
        { 
            // If GoogleId is empty or is default don't render anything.
            if (!_settings.GoogleId.HasValue() || _settings.GoogleId == "UA-0000000-0")
            {
                return Empty();
            }

            var cookiesAllowed = _cookieConsentManager.IsCookieAllowed(CookieType.Analytics);
            var globalScript = GetTrackingScript(cookiesAllowed);
            var routeData = HttpContext.GetRouteData();

            try
            {
                var controller = routeData.Values.GetControllerName();
                var action = routeData.Values.GetActionName();

                if (controller.EqualsNoCase("product") && action.EqualsNoCase("productdetails"))
                {
                    // TODO: (mh) (core) Test product detail output.
                    // Product details page
                    var viewItemScript = await GetViewItemScriptAsync((ProductDetailsModel)model);
                    globalScript = globalScript.Replace("{ECOMMERCE}", viewItemScript);
                }
                else if (controller.EqualsNoCase("catalog"))
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
                        var itemsScript = await GetListScriptAsync(productList, action.ToLower(), catId);
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
                    var lastOrder = await GetLastOrderAsync();
                    var orderCompletedScript = await GetOrderCompletedScriptAsync(lastOrder);
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

            return HtmlContent(globalScript);
        }

        // TODO: (mh) (core) Create helper for all of the methods.

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

        private async Task<string> GetViewItemScriptAsync(ProductDetailsModel model)
        {
            var brand = model.Brands.FirstOrDefault();
            var categoryId = 0; // productDetailsModel. // TODO: (mh) (core) Get category id > main cat must be set in productdetails action
            var categoryPathScript = categoryId != 0 ? await GetCategoryPathAsync(categoryId) : string.Empty;

            var productsScript = GetItemScript(
                model.Sku,
                model.Name,
                model.ProductPrice.SavingAmount.Amount.ToStringInvariant(),
                brand != null ? brand.Name : string.Empty,
                model.ProductPrice.Price.Amount.ToStringInvariant(),
                categoryPathScript);

            return @$"gtag('event', 'view_item', {{
              currency: '{Services.WorkContext.WorkingCurrency.CurrencyCode}',
              value: {model.ProductPrice.Price.Amount.ToStringInvariant()},
              'items: [{productsScript}]
            }});";
        }

        private async Task<string> GetListScriptAsync(List<ProductSummaryModel.SummaryItem> products, string listName, int categoryId = 0) 
        {
            return @$"gtag('event', 'view_item_list', {{
                item_list_name: '{listName}',
                {await GetItemsScriptAsync(products, listName, categoryId)}
            }});";
        }

        /// <summary>
        /// TODO: (mh) (core) Docs
        /// </summary>
        /// <param name="products"></param>
        /// <param name="listName">List identifier e.g. category-list, recently viewed products, etc.</param>
        /// <returns></returns>
        private async Task<string> GetItemsScriptAsync(List<ProductSummaryModel.SummaryItem> products, string listName, int categoryId = 0)
        {
            var productsScript = string.Empty;
            var categoryPathScript = categoryId != 0 ? await GetCategoryPathAsync(categoryId) : string.Empty;
            
            var i = 0;
            foreach (var product in products)
            {
                var discount = product.Price.SavingAmount;

                productsScript += GetItemScript(
                    product.Sku, 
                    product.Name, 
                    discount != null ? discount.Value.Amount.ToStringInvariant() : "0",
                    product.Brand != null ? product.Brand.Name : string.Empty,
                    product.Price.Price.Amount.ToStringInvariant(),
                    categoryPathScript,
                    listName,
                    ++i);
            }

            return $"items: [{productsScript}]";
        }

        private string GetItemScript(
            string sku,
            string productName,
            string discount,
            string brandName,
            string price,
            string categories,
            string listName = "",
            int index = 0)
        {
            return @$"{{
              item_id: '{FixIllegalJavaScriptChars(sku)}',
              item_name: '{FixIllegalJavaScriptChars(productName)}',
              currency: '{Services.WorkContext.WorkingCurrency.CurrencyCode}',
              discount: {discount},
              index: {index},
              item_brand: '{brandName}',
              {categories}
              item_list_name: '{listName}',
              price: {price}
            }},";
        }

        /// <summary>
        /// Build category tree here
        /// </summary>
        /// <param name="catId"></param>
        /// <returns>
        /// Category path in this form
        /// item_category: 'Apparel',
        /// item_category2: 'Adult',
        /// item_category3: 'Shirts',
        /// item_category4: 'Crew',
        /// item_category5: 'Short sleeve',
        /// </returns>
        private async Task<string> GetCategoryPathAsync(int catId)
        {
            var i = 0;
            var catScript = string.Empty;
            var catNode = await _categoryService.GetCategoryTreeAsync(catId, true);
            
            foreach (var node in catNode.Trail)
            {
                if (!node.IsRoot && ++i != 5)
                {
                    catScript += $"item_category{(i > 1 ? i.ToString() : string.Empty)}: '{node.Value.Name}',\n";
                }
            }

            return catScript;
        }

        private string GetTrackingScript(bool cookiesAllowed)
        {
            var script = _settings.TrackingScript + "\n";
            script = script.Replace("{GOOGLEID}", _settings.GoogleId);
            script = script.Replace("{OPTOUTCOOKIE}", GetOptOutCookieScript());

            // If no consent to third party cookies was given, set storage to none.
            script = script.Replace("{STORAGETYPE}", cookiesAllowed ? "granted" : "denied");
            script = script.Replace("{USERID}", Services.WorkContext.CurrentCustomer.CustomerGuid.ToString());

            return script;
        }

        private async Task<string> GetOrderCompletedScriptAsync(Order order)
        {
            var usCulture = new CultureInfo("en-US");
            var ecScript = _settings.EcommerceScript + '\n';

            if (order != null)
            {
                var site = Services.StoreContext.CurrentStore.Url
                    .EmptyNull()
                    .Replace("http://", string.Empty)
                    .Replace("https://", string.Empty)
                    .Replace("/", string.Empty); // TODO: (mh) (core) Kaputt!!
    
                ecScript = ecScript.Replace("{ORDERID}", order.GetOrderNumber());
                ecScript = ecScript.Replace("{SITE}", FixIllegalJavaScriptChars(site));
                // TODO: (mh) (core) Test if this can be done with ToStringInvariant
                ecScript = ecScript.Replace("{TOTAL}", order.OrderTotal.ToString("0.00", usCulture));
                ecScript = ecScript.Replace("{TAX}", order.OrderTax.ToString("0.00", usCulture));
                ecScript = ecScript.Replace("{SHIP}", order.OrderShippingInclTax.ToString("0.00", usCulture));
                ecScript = ecScript.Replace("{CURRENCY}", order.CustomerCurrencyCode);
                ecScript = ecScript.Replace("{CITY}", order.BillingAddress == null ? string.Empty : FixIllegalJavaScriptChars(order.BillingAddress.City));
                ecScript = ecScript.Replace("{STATEPROVINCE}", order.BillingAddress == null || order.BillingAddress.StateProvince == null
                    ? string.Empty
                    : FixIllegalJavaScriptChars(order.BillingAddress.StateProvince.Name));
                ecScript = ecScript.Replace("{COUNTRY}", order.BillingAddress == null || order.BillingAddress.Country == null
                    ? string.Empty
                    : FixIllegalJavaScriptChars(order.BillingAddress.Country.Name));
                
                using var psb = StringBuilderPool.Instance.Get(out var sb);
                using var writer = new StringWriter(sb);
                if (_settings.EcommerceDetailScript.HasValue())
                {
                    foreach (var item in order.OrderItems)
                    {
                        var ecDetailScript = _settings.EcommerceDetailScript;

                        // TODO: (mh) (core) This doesn't look to good :-)
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
            }

            return ecScript;
        }

        private static string FixIllegalJavaScriptChars(string text)
        {
            //replace ' with \' (http://stackoverflow.com/questions/4292761/need-to-url-encode-labels-when-tracking-events-with-google-analytics)

            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.Contains('\''))
            {
                return text.Replace("'", "\\'") ?? string.Empty;
            }

            return text;
        }



        #region Pseudo

        private void GenerateScript()
        {
            var writer = new StringWriter();
            var script = "ga.test( bla: {ECOMMERCE}; blub: {YO});";
            Parse(script, writer, new Dictionary<string, Func<string>>
            {
                ["ECOMMERCE"] = () => "Result",
                ["JADA"] = () => "Result2"
            });

            var content = writer.ToString();
        }

        private void Parse(string template, TextWriter writer, IDictionary<string, Func<string>> tokens)
        {
            var rg = new Regex("{}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

            var replacedScript = rg.Replace(template, match =>
            {
                var token = match.Value;

                if (tokens.TryGetValue(token, out var myToken))
                {
                    myToken();
                }

                return string.Empty;
            });

            writer.Write(replacedScript);
        }

        #endregion
    }
}
