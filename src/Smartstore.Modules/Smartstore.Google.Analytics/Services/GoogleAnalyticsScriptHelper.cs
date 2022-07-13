using System.Text;
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
    // TODO: (mh) (core) What you did here makes absolutely no sense. I gave up reviewing.
    // - Why do you pass the StringBuilder alongside the StringWriter?
    // - The goal was to minimize string allocations. That does not happen!
    // - The trick with TextWriter is: you create it once and pass it to all methods in your chain. These methods "contribute" some content directly INTO the writer.
    // TBD with MC.
    
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
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Generates global GA script
        /// </summary>
        /// <param name="cookiesAllowed">Defines whether cookies can be used by Google and sets ad_storage & analytics_storage of the consent tag accordingly.</param>
        public string GetTrackingScript(bool cookiesAllowed)
        {
            using var writer = new StringWriter();

            var globalTokens = new Dictionary<string, Func<string>>
            {
                ["GOOGLEID"] = () => _settings.GoogleId,
                ["OPTOUTCOOKIE"] = GetOptOutCookieScript,

                // If no consent to third party cookies was given, set storage type to denied.
                ["STORAGETYPE"] = () => cookiesAllowed ? "granted" : "denied",
                ["USERID"] = () => _workContext.CurrentCustomer.CustomerGuid.ToString()
            };
            
            ParseScript(_settings.TrackingScript, writer, globalTokens);

            return writer.ToString();
        }

        /// <summary>
        /// Generates partial script for product details view. Will be rendered after global GA script.
        /// Writes script part to fire GA event view_item.
        /// </summary>
        /// <param name="model">ProductDetailsModel already prepared by product controller for product details view.</param>
        public async Task WriteViewItemScriptAsync(TextWriter writer, StringBuilder sb, ProductDetailsModel model)
        {
            var brand = model.Brands.FirstOrDefault();
            var defaultProductCategory = (await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { model.Id })).FirstOrDefault();
            var categoryId = defaultProductCategory != null ? defaultProductCategory.Category.Id : 0;
            
            if (categoryId != 0)
            {
                await BuildCategoryPathAsync(sb, categoryId);
            }

            var categoryPathScript = sb.ToString();
            sb.Clear();

            BuildItemScript(sb,
                model.Id,
                model.Sku,
                model.Name,
                model.ProductPrice.SavingAmount.Amount.ToStringInvariant(),
                brand != null ? brand.Name : string.Empty,
                model.ProductPrice.Price.Amount.ToStringInvariant(),
                categoryPathScript);

            var eventScript = @$"gtag('event', 'view_item', {{
              currency: '{_workContext.WorkingCurrency.CurrencyCode}',
              value: {model.ProductPrice.Price.Amount.ToStringInvariant()},
              items: [{sb}]
            }});";

            sb.Clear();

            writer.Write(eventScript);
        }

        /// <summary>
        /// Generates partial script for shopping cart view. Will be rendered after global GA script.
        /// Writes script part to fire GA event view_cart.
        /// </summary>
        /// <param name="model">ShoppingCartModel already prepared by shoppingcart controller.</param>
        public async Task WriteCartScriptAsync(TextWriter writer, StringBuilder sb, ShoppingCartModel model)
        {
            var currency = _workContext.WorkingCurrency;
            var cart = await _shoppingCartService.GetCartAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            BuildShoppingCartItemsScript(sb, model.Items.ToList());
            var cartItemsScript = sb.ToString();
            sb.Clear();

            // TODO: (mh) (core) Create GetEventScript method (nearly every event script looks the same)?
            var eventScript = @$"
                let eventDataCart = {cartItemsScript};

                window.gaListDataStore.push(eventDataCart);

                gtag('event', 'view_cart', {{
                    currency: '{currency.CurrencyCode}',
                    value: {subTotalConverted.Amount.ToStringInvariant()},
                    items: eventDataCart
                }});";

            writer.Write(eventScript);
        }

        /// <summary>
        /// Generates partial script for billing address, payment and shipping selection pages. 
        /// Will be rendered after global GA script.
        /// Writes script part to fire GA event begin_checkout, add_shipping_info or add_payment_info.
        /// </summary>
        /// <param name="addShippingInfo">Specifies whether shipping_tier property shoud be added to the event. True if we are on payment selection page.</param>
        /// <param name="addPaymentInfo">Specifies whether payment_type property shoud be added to the event. True if we are on payment selection page.</param>
        public async Task WriteCheckoutScriptAsync(TextWriter writer, StringBuilder sb, bool addShippingInfo = false, bool addPaymentInfo = false)
        {
            var currency = _workContext.WorkingCurrency;
            var cart = await _shoppingCartService.GetCartAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            var model = await cart.MapAsync();
            BuildShoppingCartItemsScript(sb, model.Items.ToList());
            var cartItemsScript = sb.ToString();
            sb.Clear();

            addPaymentInfo = addPaymentInfo && model.OrderReviewData.PaymentMethod.HasValue();
            addShippingInfo = addShippingInfo && model.OrderReviewData.ShippingMethod.HasValue();

            var eventType = "begin_checkout";
            if (addShippingInfo) eventType = "add_shipping_info";
            if (addPaymentInfo) eventType = "add_payment_info";

            var eventScript = @$"
                let eventDataCart = {cartItemsScript};

                window.gaListDataStore.push(eventDataCart);

                gtag('event', '{eventType}', {{
                    currency: '{currency.CurrencyCode}',
                    value: {subTotalConverted.Amount.ToStringInvariant()},
                    coupon: '{model.DiscountBox.CurrentCode}',
                    {(addShippingInfo ? $"shipping_tier: '{model.OrderReviewData.ShippingMethod}'," : string.Empty)}
                    {(addPaymentInfo ? $"payment_type: '{model.OrderReviewData.PaymentMethod}'," : string.Empty)}
                    items: eventDataCart
                }});
            ";

            writer.Write(eventScript);
        }

        /// <summary>
        /// Generates partial script for shopping cart items. 
        /// Will be rendered for checkout events begin_checkout, add_shipping_info, add_payment_info & view_cart.
        /// Builds e.g.: [{item_id: "SKU_12345",...}, {...}, n]
        /// </summary>
        /// <param name="products">List of ShoppingCartItemModel</param>
        private void BuildShoppingCartItemsScript(StringBuilder sb, List<ShoppingCartModel.ShoppingCartItemModel> products)
        {
            var i = 0;
            foreach (var product in products)
            {
                BuildItemScript(
                    sb,
                    product.Id,
                    product.Sku,
                    product.ProductName,
                    product.Discount.Amount.ToStringInvariant(),
                    string.Empty,
                    product.UnitPrice.Amount.ToStringInvariant(),
                    index: ++i);
            }

            var items = sb.ToString();
            sb.Clear();
            sb.AppendLine($"[{items}]");
        }

        /// <summary>
        /// Builds json properties for the category tree as defined by Google.
        /// Builds category path in this form:
        /// 
        /// item_category: 'Apparel',
        /// item_category2: 'Adult',
        /// item_category3: 'Shirts',
        /// item_category4: 'Crew',
        /// item_category5: 'Short sleeve',
        /// </summary>
        /// <param name="catId">Id of the target category</param>
        private async Task BuildCategoryPathAsync(StringBuilder sb, int catId)
        {
            var i = 0;
            var catNode = await _categoryService.GetCategoryTreeAsync(catId, true);
            
            foreach (var node in catNode.Trail)
            {
                if (!node.IsRoot && ++i != 5)
                {
                    sb.AppendLine($"item_category{(i > 1 ? i.ToString() : string.Empty)}: '{node.Value.Name}',");
                }
            }
        }

        /// <summary>
        /// Generates partial script for product lists. Used for pages (eg. category, manufacturer) as well as for view components (e.g. HomepageBestsellers, RecentlyViewedProducts)
        /// Will be rendered after global GA script.
        /// Writes script part to fire GA event view_item_list.
        /// </summary>
        /// <param name="products"><see cref="List<ProductSummaryModel.SummaryItem>"/> already prepared by category controller.</param>
        /// <param name="listName">List identifier (action or view component name e.g. category-list, RecentlyViewedProducts, etc.).</param>
        /// <param name="categoryId">First category of the product, when called form category view.</param>
        public async Task WriteListScriptAsync(TextWriter writer, StringBuilder sb, List<ProductSummaryModel.SummaryItem> products, string listName, int categoryId = 0)
        {
            await BuildItemsScriptAsync(sb, products, listName, categoryId);
            var items = sb.ToString();
            sb.Clear();

            var eventScript = @$"
                let eventData{listName} = {{
                    item_list_name: '{listName}',
                    {items}
                }}

                window.gaListDataStore.push(eventData{listName});
                gtag('event', 'view_item_list', eventData{listName});
            ";

            writer.Write(eventScript);
        }

        /// <summary>
        /// Generates script for items property of view_item_list event.
        /// Used by WriteListScriptAsync.
        /// Builds e.g.: items: [{item_id: "SKU_12345",...}, {...}, n]
        /// </summary>
        /// <param name="products"><see cref="List<ProductSummaryModel.SummaryItem>"/> already prepared by category controller.</param>
        /// <param name="listName">List identifier (action or view component name e.g. category-list, RecentlyViewedProducts, etc.).</param>
        /// <param name="categoryId">First category of the product, when called form category view.</param>
        private async Task BuildItemsScriptAsync(StringBuilder sb, List<ProductSummaryModel.SummaryItem> products, string listName, int categoryId = 0)
        {
            if (categoryId != 0)
            {
                await BuildCategoryPathAsync(sb, categoryId);
            }

            var categoryPathScript = sb.ToString();
            sb.Clear();

            var i = 0;
            foreach (var product in products)
            {
                var discount = product.Price.SavingAmount;

                BuildItemScript(
                    sb,
                    product.Id,
                    product.Sku,
                    product.Name,
                    discount != null ? discount.Value.Amount.ToStringInvariant() : "0",
                    product.Brand != null ? product.Brand.Name : string.Empty,
                    product.Price.Price.Amount.ToStringInvariant(),
                    categoryPathScript,
                    listName,
                    ++i);
            }

            var items = sb.ToString();
            sb.Clear();
            sb.AppendLine($"items: [{items}]");
        }

        /// <summary>
        /// Generates partial script for one item of items property. Inclusive comma.
        /// Builds e.g.: {item_id: "SKU_12345",...},
        /// </summary>
        private void BuildItemScript(
            StringBuilder sb,
            int entityId,
            string sku,
            string productName,
            string discount,
            string brandName,
            string price,
            string categories = "",
            string listName = "",
            int index = 0)
        {
            sb.AppendLine(@$"{{
              entity_id: {entityId},
              item_id: '{FixIllegalJavaScriptChars(sku)}',
              item_name: '{FixIllegalJavaScriptChars(productName)}',
              currency: '{_workContext.WorkingCurrency.CurrencyCode}',
              discount: {discount},
              index: {index},
              item_brand: '{brandName}',
              {categories}
              item_list_name: '{listName}',
              price: {price}
            }},");
        }

        /// <summary>
        /// Generates partial script for search page. 
        /// Will be rendered after global GA script.
        /// Writes script part to fire GA event search.
        /// </summary>
        public void WriteSearchTermScript(TextWriter writer, string searchTerm)
        {
            var eventScript = @$"
                gtag('event', 'search', {{
                  search_term: '{searchTerm}'
                }});
            ";

            writer.Write(eventScript);
        }

        /// <summary>
        /// Generates partial script for order completed page. 
        /// Will be rendered after global GA script.
        /// Writes script part to fire GA event purchase.
        /// </summary>
        public async Task WriteOrderCompletedScriptAsync(TextWriter writer, StringBuilder sb)
        {
            var order = await GetLastOrderAsync();
            
            if (order != null)
            {
                var ecScript = _settings.EcommerceScript + '\n';
                
                if (_settings.EcommerceDetailScript.HasValue())
                {
                    var productIds = order.OrderItems.Select(x => x.ProductId).ToArray();
                    var categories = (await _categoryService.GetProductCategoriesByProductIdsAsync(productIds))
                        .ToDictionarySafe(x => x.ProductId);

                    foreach (var item in order.OrderItems)
                    {
                        var defaultProductCategory = categories[item.ProductId];
                        var categoryName = defaultProductCategory != null ? defaultProductCategory.Category?.Name : string.Empty;

                        // The SKU code is a required parameter for every item that is added to the transaction.
                        var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(item.ProductId, item.AttributeSelection);
                        if (attributeCombination != null)
                        {
                            item.Product.MergeWithCombination(attributeCombination);
                        }

                        var itemTokens = new Dictionary<string, Func<string>>
                        {
                            ["ORDERID"] = () => order.GetOrderNumber(),
                            ["PRODUCTSKU"] = () => FixIllegalJavaScriptChars(item.Product.Sku),
                            ["PRODUCTNAME"] = () => FixIllegalJavaScriptChars(item.Product.Name),
                            ["CATEGORYNAME"] = () => FixIllegalJavaScriptChars(categoryName),
                            ["UNITPRICE"] = () => item.UnitPriceInclTax.ToStringInvariant("0.00"),
                            ["QUANTITY"] = () => item.Quantity.ToString()
                        };

                        var ecDetailScript = GenerateScript(_settings.EcommerceDetailScript, itemTokens);

                        sb.AppendLine(ecDetailScript);
                    }
                }

                var orderTokens = new Dictionary<string, Func<string>>
                {
                    ["ORDERID"] = () => order.GetOrderNumber(),
                    ["TOTAL"] = () => order.OrderTotal.ToStringInvariant("0.00"),
                    ["TAX"] = () => order.OrderTax.ToStringInvariant("0.00"),
                    ["SHIP"] = () => order.OrderShippingInclTax.ToStringInvariant("0.00"),
                    ["CURRENCY"] = () => order.CustomerCurrencyCode,
                    ["CITY"] = () => order.BillingAddress == null ? string.Empty : FixIllegalJavaScriptChars(order.BillingAddress.City),
                    ["STATEPROVINCE"] = () => order.BillingAddress == null || order.BillingAddress.StateProvince == null
                        ? string.Empty
                        : FixIllegalJavaScriptChars(order.BillingAddress.StateProvince.Name),
                    ["COUNTRY"] = () => order.BillingAddress == null || order.BillingAddress.Country == null
                        ? string.Empty
                        : FixIllegalJavaScriptChars(order.BillingAddress.Country.Name),
                    ["DETAILS"] = () => sb.ToString()
                };

                ParseScript(ecScript, writer, orderTokens);
            }
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

        // TODO: (mh) (core) Maybe we don't need two methods for this.
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