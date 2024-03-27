using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Google.Analytics.Services
{
    /// <summary>
    /// Helper class to prepare script parts for Google Analytics (GA) according to 
    /// https://developers.google.com/analytics/devguides/collection/ga4/ecommerce?client_type=gtag
    /// </summary>
    public class GoogleAnalyticsScriptHelper
    {
        private readonly static Regex _rgScript = new(@"{(?<token>([A-Z]+))}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private readonly SmartDbContext _db;
        private readonly GoogleAnalyticsSettings _settings;
        private readonly ICategoryService _categoryService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICurrencyService _currencyService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public GoogleAnalyticsScriptHelper(
            SmartDbContext db,
            GoogleAnalyticsSettings settings,
            ICategoryService categoryService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IOrderCalculationService orderCalculationService,
            IShoppingCartService shoppingCartService,
            ICurrencyService currencyService,
            IRoundingHelper roundingHelper,
            IWorkContext workContext,
            IStoreContext storeContext)
        {
            _db = db;
            _settings = settings;
            _categoryService = categoryService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _orderCalculationService = orderCalculationService;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _roundingHelper = roundingHelper;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Generates global GA script
        /// </summary>
        /// <param name="cookiesAllowed">Defines whether cookies can be used by Google and sets ad_storage & analytics_storage of the consent tag accordingly.</param>
        public string GetTrackingScript(bool cookiesAllowed, bool adUserDataAllowed, bool adPersonalizationAllowed)
        {
            using var writer = new StringWriter();

            var globalTokens = new Dictionary<string, Func<string>>
            {
                ["GOOGLEID"] = () => _settings.GoogleId,
                ["OPTOUTCOOKIE"] = GetOptOutCookieScript,

                // If no consent to third party cookies was given, set storage type to denied.
                ["STORAGETYPE"] = () => cookiesAllowed ? "granted" : "denied",
                ["USERID"] = _workContext.CurrentCustomer.CustomerGuid.ToString,
                ["ADUSERDATA"] = () => adUserDataAllowed ? "granted" : "denied",
                ["ADPERSONALIZATION"] = () => adPersonalizationAllowed ? "granted" : "denied"
            };

            ParseScript(_settings.TrackingScript, writer, globalTokens);

            return writer.ToString();
        }

        /// <summary>
        /// Generates partial script for product details view. Will be rendered after global GA script.
        /// </summary>
        /// <param name="model">ProductDetailsModel already prepared by product controller for product details view.</param>
        /// <returns>Script part to fire GA event view_item</returns>
        public async Task<string> GetViewItemScriptAsync(ProductDetailsModel model)
        {
            var brand = model.Brands.FirstOrDefault();
            var defaultProductCategory = (await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { model.Id })).FirstOrDefault();
            var categoryId = defaultProductCategory != null ? defaultProductCategory.Category.Id : 0;
            var categoryPathScript = categoryId != 0 ? await GetCategoryPathAsync(categoryId) : string.Empty;
            var price = _roundingHelper.Round(model.Price.FinalPrice).ToStringInvariant();
            
            var productsScript = GetItemScript(
                model.Id,
                model.Sku,
                model.Name,
                !model.Price.HasDiscount ? "''" : _roundingHelper.Round(model.Price.Saving.SavingAmount.Value).ToStringInvariant(),
                brand != null ? brand.Name : string.Empty,
                price,
                categoryPathScript, addComma: false);

            var eventScript = @$"
                let pdItem = {productsScript};
                let list = {{
                    item_list_name: 'product-detail',
                    items: [pdItem]
                }}

                window.gaListDataStore.push(list);
            
                gtag('event', 'view_item', {{
                  currency: '{_workContext.WorkingCurrency.CurrencyCode}',
                  value: {price},
                  items: [pdItem]
                }});";

            return eventScript;
        }

        /// <summary>
        /// Generates partial script for shopping cart view. Will be rendered after global GA script.
        /// </summary>
        /// <param name="model">ShoppingCartModel already prepared by shoppingcart controller.</param>
        /// <returns>Script part to fire GA event view_cart</returns>
        public async Task<string> GetCartScriptAsync(ShoppingCartModel model)
        {
            var cart = await _shoppingCartService.GetCartAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
            var subtotal = await GetSubtotal(cart);
            var cartItemsScript = GetShoppingCartItemsScript(model.Items.ToList());

            return @$"
                let cartItems = {cartItemsScript};

                let cartItemList = {{
                    item_list_name: 'cart',
                    items: cartItems
                }}

                window.gaListDataStore.push(cartItemList);

                gtag('event', 'view_cart', {{
                    currency: '{_workContext.WorkingCurrency.CurrencyCode}',
                    value: {subtotal.ToStringInvariant()},
                    items: cartItems
                }});";
        }

        /// <summary>
        /// Generates partial script for billing address, payment and shipping selection pages. 
        /// Will be rendered after global GA script.
        /// </summary>
        /// <param name="addShippingInfo">Specifies whether shipping_tier property shoud be added to the event. True if we are on payment selection page.</param>
        /// <param name="addPaymentInfo">Specifies whether payment_type property shoud be added to the event. True if we are on payment selection page.</param>
        /// <returns>Script part to fire GA event begin_checkout, add_shipping_info or add_payment_info</returns>
        public async Task<string> GetCheckoutScriptAsync(bool addShippingInfo = false, bool addPaymentInfo = false)
        {
            var cart = await _shoppingCartService.GetCartAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
            var subtotal = await GetSubtotal(cart);

            var model = await cart.MapAsync();
            var cartItemsScript = GetShoppingCartItemsScript(model.Items.ToList());

            addPaymentInfo = addPaymentInfo && model.OrderReviewData.PaymentMethod.HasValue();
            addShippingInfo = addShippingInfo && model.OrderReviewData.ShippingMethod.HasValue();

            var eventType = "begin_checkout";
            if (addShippingInfo) eventType = "add_shipping_info";
            if (addPaymentInfo) eventType = "add_payment_info";

            return @$"
                let cartItems = {cartItemsScript};

                gtag('event', '{eventType}', {{
                    currency: '{_workContext.WorkingCurrency.CurrencyCode}',
                    value: {subtotal.ToStringInvariant()},
                    coupon: '{model.DiscountBox.CurrentCode}',
                    {(addShippingInfo ? $"shipping_tier: '{model.OrderReviewData.ShippingMethod}'," : string.Empty)}
                    {(addPaymentInfo ? $"payment_type: '{model.OrderReviewData.PaymentMethod}'," : string.Empty)}
                    items: cartItems
                }});
            ";
        }

        private async Task<decimal> GetSubtotal(ShoppingCart cart)
        {
            var cartSubtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var subtotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubtotal.SubtotalWithoutDiscount.Amount, _workContext.WorkingCurrency);

            return _roundingHelper.Round(subtotalConverted);
        }

        /// <summary>
        /// Generates partial script for shopping cart items. 
        /// Will be rendered for checkout events begin_checkout, add_shipping_info, add_payment_info & view_cart.
        /// </summary>
        /// <param name="products">List of ShoppingCartItemModel</param>
        /// <returns>e.g.: items: [{item_id: "SKU_12345",...}, {...}, n] </returns>
        private string GetShoppingCartItemsScript(List<ShoppingCartModel.ShoppingCartItemModel> products)
        {
            var productsScript = string.Empty;

            var i = 0;
            foreach (var product in products)
            {
                productsScript += GetItemScript(
                    product.Id,
                    product.Sku,
                    product.ProductName,
                    product.Price.Saving.SavingAmount.HasValue ? _roundingHelper.Round(product.Price.Saving.SavingAmount.Value).ToStringInvariant() : "0",
                    string.Empty,
                    _roundingHelper.Round(product.Price.UnitPrice).ToStringInvariant(),
                    index: ++i);
            }

            return $"[{productsScript}]";
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
        private async Task<string> GetCategoryPathAsync(int catId)
        {
            var i = 0;
            var catScript = string.Empty;
            var catNode = await _categoryService.GetCategoryTreeAsync(catId, true);

            foreach (var node in catNode.Trail)
            {
                if (!node.IsRoot && ++i != 5)
                {
                    catScript += $"item_category{(i > 1 ? i.ToString() : string.Empty)}: '{FixIllegalJavaScriptChars(node.Value.Name)}',";
                }
            }

            return catScript;
        }

        /// <summary>
        /// Generates partial script for product lists. Used for pages (eg. category, manufacturer) as well as for view components (e.g. HomepageBestsellers, RecentlyViewedProducts)
        /// Will be rendered after global GA script.
        /// </summary>
        /// <param name="products"><see cref="List<ProductSummaryModel.SummaryItem>"/> already prepared by category controller.</param>
        /// <param name="listName">List identifier (action or view component name e.g. category-list, RecentlyViewedProducts, etc.).</param>
        /// <param name="categoryId">First category of the product, when called form category view.</param>
        /// <returns>Script part to fire GA event view_item_list</returns>
        public async Task<string> GetListScriptAsync(List<ProductSummaryItemModel> products, string listName, int categoryId = 0)
        {
            listName = FixIllegalJavaScriptChars(listName);
            return @$"
                let eventData{listName} = {{
                    item_list_name: '{listName}',
                    {await GetItemsScriptAsync(products, listName, categoryId)}
                }}

                window.gaListDataStore.push(eventData{listName});
                gtag('event', 'view_item_list', eventData{listName});
            ";
        }

        /// <summary>
        /// Generates script for items property of view_item_list event.
        /// Used by GetListScriptAsync.
        /// </summary>
        /// <param name="products"><see cref="List<ProductSummaryModel.SummaryItem>"/> already prepared by category controller.</param>
        /// <param name="listName">List identifier (action or view component name e.g. category-list, RecentlyViewedProducts, etc.).</param>
        /// <param name="categoryId">First category of the product, when called form category view.</param>
        /// <returns>e.g.: items: [{item_id: "SKU_12345",...}, {...}, n] </returns>
        private async Task<string> GetItemsScriptAsync(List<ProductSummaryItemModel> products, string listName, int categoryId = 0)
        {
            var productsScript = string.Empty;
            var categoryPathScript = categoryId != 0 ? await GetCategoryPathAsync(categoryId) : string.Empty;

            var i = 0;
            foreach (var product in products)
            {
                var discount = product.Price.Saving.SavingAmount;
                
                productsScript += GetItemScript(
                    product.Id,
                    product.Sku,
                    product.Name,
                    discount != null ? _roundingHelper.Round(discount.Value).ToStringInvariant() : "0",
                    product.Brand != null ? product.Brand.Name : string.Empty,
                    _roundingHelper.Round(product.Price.FinalPrice).ToStringInvariant(),
                    categoryPathScript,
                    listName,
                    ++i);
            }

            return $"items: [{productsScript}]";
        }

        /// <summary>
        /// Generates partial script for one item of items property. Inclusive comma.
        /// </summary>
        /// <returns>e.g.: {item_id: "SKU_12345",...},</returns>
        private string GetItemScript(
            int entityId,
            string sku,
            string productName,
            string discount,
            string brandName,
            string price,
            string categories = "",
            string listName = "",
            int index = 0,
            bool addComma = true)
        {
            var itemScript = @$"{{
              entity_id: {entityId},
              item_id: '{FixIllegalJavaScriptChars(sku)}',
              item_name: '{FixIllegalJavaScriptChars(productName)}',
              currency: '{_workContext.WorkingCurrency.CurrencyCode}',
              discount: {discount},
              index: {index},
              {categories}
              price: {price},
              {(!string.IsNullOrEmpty(listName) ? $"item_list_name: '{listName}'," : string.Empty)}
              {(!string.IsNullOrEmpty(brandName) ? $"item_brand: '{brandName}'," : string.Empty)}
            }}";

            if (addComma)
            {
                itemScript += ",";
            }

            return itemScript;
        }

        /// <summary>
        /// Generates partial script for search page. 
        /// Will be rendered after global GA script.
        /// </summary>
        /// <returns>Script part to fire GA event search.</returns>
        public string GetSearchTermScript(string searchTerm)
        {
            return @$"
                gtag('event', 'search', {{
                  search_term: '{searchTerm}'
                }});
            ";
        }

        /// <summary>
        /// Generates partial script for order completed page. 
        /// Will be rendered after global GA script.
        /// </summary>
        /// <returns>Script part to fire GA event purchase.</returns>
        public async Task<string> GetOrderCompletedScriptAsync()
        {
            var order = await GetLastOrderAsync();
            var ecScript = _settings.EcommerceScript + '\n';

            if (order != null)
            {
                var ecDetailScript = string.Empty;

                if (_settings.EcommerceDetailScript.HasValue())
                {
                    var productIds = order.OrderItems.ToDistinctArray(x => x.ProductId);
                    var categories = (await _categoryService.GetProductCategoriesByProductIdsAsync(productIds, true))
                        .ToDictionarySafe(x => x.ProductId);

                    foreach (var item in order.OrderItems)
                    {
                        categories.TryGetValue(item.ProductId, out var defaultProductCategory);
                        var categoryName = defaultProductCategory?.Category?.Name ?? string.Empty;

                        // The SKU code is a required parameter for every item that is added to the transaction.
                        var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(item.ProductId, item.AttributeSelection);
                        if (attributeCombination != null)
                        {
                            item.Product.MergeWithCombination(attributeCombination);
                        }

                        var sku = item.Sku.NullEmpty() ?? item.Product.Sku;

                        var itemTokens = new Dictionary<string, Func<string>>
                        {
                            ["ORDERID"] = order.GetOrderNumber,
                            ["PRODUCTSKU"] = () => FixIllegalJavaScriptChars(sku),
                            ["PRODUCTNAME"] = () => FixIllegalJavaScriptChars(item.Product.Name),
                            ["CATEGORYNAME"] = () => FixIllegalJavaScriptChars(categoryName),
                            ["UNITPRICE"] = () => item.UnitPriceInclTax.ToStringInvariant("0.00"),
                            ["QUANTITY"] = item.Quantity.ToString
                        };

                        ecDetailScript += GenerateScript(_settings.EcommerceDetailScript, itemTokens);
                    }
                }

                var orderTokens = new Dictionary<string, Func<string>>
                {
                    ["ORDERID"] = order.GetOrderNumber,
                    ["TOTAL"] = () => order.OrderTotal.ToStringInvariant("0.00"),
                    ["TAX"] = () => order.OrderTax.ToStringInvariant("0.00"),
                    ["SHIP"] = () => order.OrderShippingInclTax.ToStringInvariant("0.00"),
                    ["CURRENCY"] = () => order.CustomerCurrencyCode,
                    ["CITY"] = () => order.BillingAddress == null 
                        ? string.Empty 
                        : FixIllegalJavaScriptChars(order.BillingAddress.City),
                    ["STATEPROVINCE"] = () => order.BillingAddress?.StateProvince == null
                        ? string.Empty
                        : FixIllegalJavaScriptChars(order.BillingAddress.StateProvince.Name),
                    ["COUNTRY"] = () => order.BillingAddress?.Country == null
                        ? string.Empty
                        : FixIllegalJavaScriptChars(order.BillingAddress.Country.Name),
                    ["DETAILS"] = () => ecDetailScript
                };

                ecScript = GenerateScript(ecScript, orderTokens);
            }

            return ecScript;
        }

        /// <summary>
        /// Builds script to provide functions for cookie usage opt-out. Usage is described in AdminInstruction.
        /// </summary>
        private string GetOptOutCookieScript()
        {
            return @$"
				var gaProperty = '{_settings.GoogleId}'; 
				var disableStr = 'ga-disable-' + gaProperty; 
				if (document.cookie.indexOf(disableStr + '=true') > -1) {{ 
					window[disableStr] = true;
				}} 
				function gaOptout() {{ 
					document.cookie = disableStr + '=true; expires=Thu, 31 Dec 2099 23:59:59 UTC; path=/'; 
					window[disableStr] = true; 
					alert({T("Plugins.Widgets.GoogleAnalytics.OptOutNotification").JsValue});
                }}";
        }

        private static string GenerateScript(string script, Dictionary<string, Func<string>> tokens)
        {
            var writer = new StringWriter();
            ParseScript(script, writer, tokens);
            return writer.ToString();
        }

        private static void ParseScript(string input, TextWriter writer, IDictionary<string, Func<string>> tokens)
        {
            var replacedScript = _rgScript.Replace(input, match =>
            {
                var token = match.Groups[1].Value;

                if (tokens.TryGetValue(token, out var func))
                {
                    return func();
                }

                // If no func is found we return token in order to not remove any tokens which shouldn't replaced explicitly.
                return match.Value;
            });

            writer.Write(replacedScript);
        }

        private async Task<Order> GetLastOrderAsync()
        {
            var order = await _db.Orders
                .AsNoTracking()
                .IncludeOrderItems()
                .IncludeBillingAddress()
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