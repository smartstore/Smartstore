using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Google.Analytics.Settings;
using Smartstore.Utilities;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Google.Analytics.Services
{
    public class GoogleAnalyticsScriptHelper
    {
        private readonly SmartDbContext _db;
        private readonly GoogleAnalyticsSettings _settings;
        private readonly ICategoryService _categoryService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public GoogleAnalyticsScriptHelper(
            SmartDbContext db, 
            GoogleAnalyticsSettings settings, 
            ICategoryService categoryService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IWorkContext workContext,
            IStoreContext storeContext)
        {
            _db = db;
            _settings = settings;
            _categoryService = categoryService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public string GetTrackingScript(bool cookiesAllowed)
        {
            var script = _settings.TrackingScript + "\n";
            script = script.Replace("{GOOGLEID}", _settings.GoogleId);
            script = script.Replace("{OPTOUTCOOKIE}", GetOptOutCookieScript());

            // If no consent to third party cookies was given, set storage to none.
            script = script.Replace("{STORAGETYPE}", cookiesAllowed ? "granted" : "denied");
            script = script.Replace("{USERID}", _workContext.CurrentCustomer.CustomerGuid.ToString());

            return script;
        }

        public async Task<string> GetViewItemScriptAsync(ProductDetailsModel model)
        {
            var brand = model.Brands.FirstOrDefault();
            var defaultProductCategory = (await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { model.Id })).FirstOrDefault();
            var categoryId = defaultProductCategory != null ? defaultProductCategory.Category.Id : 0;
            var categoryPathScript = categoryId != 0 ? await GetCategoryPathAsync(categoryId) : string.Empty;

            var productsScript = GetItemScript(
                model.Sku,
                model.Name,
                model.ProductPrice.SavingAmount.Amount.ToStringInvariant(),
                brand != null ? brand.Name : string.Empty,
                model.ProductPrice.Price.Amount.ToStringInvariant(),
                categoryPathScript);

            return @$"gtag('event', 'view_item', {{
              currency: '{_workContext.WorkingCurrency.CurrencyCode}',
              value: {model.ProductPrice.Price.Amount.ToStringInvariant()},
              'items: [{productsScript}]
            }});";
        }

        /// <summary>
        /// Builds json properties for the category tree as defined by Google.
        /// </summary>
        /// <param name="catId">Id of the target category</param>
        /// <returns>
        /// Category path in this form:
        /// 
        /// item_category: 'Apparel',
        /// item_category2: 'Adult',
        /// item_category3: 'Shirts',
        /// item_category4: 'Crew',
        /// item_category5: 'Short sleeve',
        /// </returns>
        public async Task<string> GetCategoryPathAsync(int catId)
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

        /// <summary>
        /// Build script for product lists. Used for pages (eg. category, manufacturer) as well as for view components (e.g. HomepageBestsellers, RecentlyViewedProducts)
        /// </summary>
        public async Task<string> GetListScriptAsync(List<ProductSummaryModel.SummaryItem> products, string listName, int categoryId = 0)
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

        public string GetItemScript(
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
              currency: '{_workContext.WorkingCurrency.CurrencyCode}',
              discount: {discount},
              index: {index},
              item_brand: '{brandName}',
              {categories}
              item_list_name: '{listName}',
              price: {price}
            }},";
        }

        public async Task<string> GetOrderCompletedScriptAsync()
        {
            var order = await GetLastOrderAsync();
            var ecScript = _settings.EcommerceScript + '\n';

            if (order != null)
            {
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

                        var itemTokens = new Dictionary<string, Func<string>>
                        {
                            ["{ORDERID}"] = () => order.GetOrderNumber(),
                            ["{PRODUCTSKU}"] = () => FixIllegalJavaScriptChars(item.Product.Sku),
                            ["{PRODUCTNAME}"] = () => FixIllegalJavaScriptChars(item.Product.Name),
                            ["{CATEGORYNAME}"] = () => FixIllegalJavaScriptChars(categoryName),
                            ["{UNITPRICE}"] = () => item.UnitPriceInclTax.ToStringInvariant("0.00"),
                            ["{QUANTITY}"] = () => item.Quantity.ToString()
                        };

                        ecDetailScript = GenerateScript(ecDetailScript, itemTokens);

                        sb.AppendLine(ecDetailScript);
                    }
                }

                ecScript = ecScript.Replace("{DETAILS}", sb.ToString());
                var orderTokens = new Dictionary<string, Func<string>>
                {
                    ["{ORDERID}"] = () => order.GetOrderNumber(),
                    ["{TOTAL}"] = () => order.OrderTotal.ToStringInvariant("0.00"),
                    ["{TAX}"] = () => order.OrderTax.ToStringInvariant("0.00"),
                    ["{SHIP}"] = () => order.OrderShippingInclTax.ToStringInvariant("0.00"),
                    ["{CURRENCY}"] = () => order.CustomerCurrencyCode,
                    ["{CITY}"] = () => order.BillingAddress == null ? string.Empty : FixIllegalJavaScriptChars(order.BillingAddress.City),
                    ["{STATEPROVINCE}"] = () => order.BillingAddress == null || order.BillingAddress.StateProvince == null
                        ? string.Empty
                        : FixIllegalJavaScriptChars(order.BillingAddress.StateProvince.Name),
                    ["{COUNTRY}"] = () => order.BillingAddress == null || order.BillingAddress.Country == null
                        ? string.Empty
                        : FixIllegalJavaScriptChars(order.BillingAddress.Country.Name),
                    ["{DETAILS}"] = () => sb.ToString()
                };

                ecScript = GenerateScript(ecScript, orderTokens);
            }

            return ecScript;
        }

        private string GetOptOutCookieScript()
        {
            var script = @$"
				var gaProperty = '{_settings.GoogleId}'; 
				var disableStr = 'ga-disable-' + gaProperty; 
				if (document.cookie.indexOf(disableStr + '=true') > -1) {{ 
					window[disableStr] = true;
				}} 
				function gaOptout() {{ 
					document.cookie = disableStr + '=true; expires=Thu, 31 Dec 2099 23:59:59 UTC; path=/'; 
					window[disableStr] = true; 
					alert({T("Plugins.Widgets.GoogleAnalytics.OptOutNotification").JsValue});
                }}
			";

            return script;
        }

        // TODO: (mh) (core) Maybe we don't need two methods for this.
        private static string GenerateScript(string script, Dictionary<string, Func<string>> tokens)
        {
            var writer = new StringWriter();
            Parse(script, writer, tokens);
            return writer.ToString();
        }

        private static void Parse(string template, TextWriter writer, IDictionary<string, Func<string>> tokens)
        {
            var rg = new Regex(@"{(?<token>([A-Z]+))}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

            var replacedScript = rg.Replace(template, match =>
            {
                var token = match.Value;

                if (tokens.TryGetValue(token, out var func))
                {
                    return func();
                }

                return string.Empty;
            });

            writer.Write(replacedScript);
        }

        private async Task<Order> GetLastOrderAsync()
        {
            var order = await _db.Orders
                .ApplyStandardFilter(_workContext.CurrentCustomer.Id, _storeContext.CurrentStore.Id)
                .FirstOrDefaultAsync();

            return order;
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
    }
}