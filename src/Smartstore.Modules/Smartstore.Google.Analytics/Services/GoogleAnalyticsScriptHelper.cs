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
using Smartstore.Google.Analytics.Settings;
using Smartstore.Utilities;
using Smartstore.Web.Models.Cart;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Google.Analytics.Services
{
    // TODO: (mh) (core) Use StringWriter alongside the whole script generation chain.
    // Don't mix up strings and writer, you win nothing.
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

        public string GetTrackingScript(bool cookiesAllowed)
        {
            var framework = @"<script>window.eventDataStore = [];</script>" + "\n";
            var script = framework + _settings.TrackingScript + "\n";
            script = script.Replace("{GOOGLEID}", _settings.GoogleId);
            script = script.Replace("{OPTOUTCOOKIE}", GetOptOutCookieScript());

            // If no consent to third party cookies was given, set storage type to denied.
            script = script.Replace("{STORAGETYPE}", cookiesAllowed ? "granted" : "denied");
            script = script.Replace("{USERID}", _workContext.CurrentCustomer.CustomerGuid.ToString());

            return script;
        }

        // TODO: (mh) (core) Order methods.

        public async Task<string> GetViewItemScriptAsync(ProductDetailsModel model)
        {
            var brand = model.Brands.FirstOrDefault();
            var defaultProductCategory = (await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { model.Id })).FirstOrDefault();
            var categoryId = defaultProductCategory != null ? defaultProductCategory.Category.Id : 0;
            var categoryPathScript = categoryId != 0 ? await GetCategoryPathAsync(categoryId) : string.Empty;

            var productsScript = GetItemScript(
                model.Id,
                model.Sku,
                model.Name,
                model.ProductPrice.SavingAmount.Amount.ToStringInvariant(),
                brand != null ? brand.Name : string.Empty,
                model.ProductPrice.Price.Amount.ToStringInvariant(),
                categoryPathScript);

            return @$"gtag('event', 'view_item', {{
              currency: '{_workContext.WorkingCurrency.CurrencyCode}',
              value: {model.ProductPrice.Price.Amount.ToStringInvariant()},
              items: [{productsScript}]
            }});";
        }

        public async Task<string> GetCartScriptAsync(ShoppingCartModel model)
        {
            var currency = _workContext.WorkingCurrency;
            var customer = _workContext.CurrentCustomer;
            var storeId = _storeContext.CurrentStore.Id;

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            var productsScript = GetShoppingCartItemsScript(model.Items.ToList());

            // TODO: (mh) (core) Create GetEventScript method (nearly every event script looks the same)?
            return @$"
                // Begin checkout script
                let eventDataCart = {productsScript}

                window.eventDataStore.push(eventDataCart);

                gtag('event', 'view_cart', {{
                    currency: '{currency}',
                    value: {subTotalConverted.Amount.ToStringInvariant()},
                    items: eventDataCart
                }});

                // Remove cart item script
                $(function () {{
                    // There's only one product list on cart page
                    let list = window.eventDataStore[0];
                    $('.cart-body').on('click', '[data-type=""cart""]', function (e) {{
                        var $el = $(e.target);
                        var btn = $el.closest('.btn') || $el;
                        var id = btn.data('id');

                        let item = list.filter(function(obj) {{
	                        return obj.entity_id === id;
                        }});

                        // Fire event
                        gtag('event', 'remove_from_cart', {{
                            item_list_name: item[0].item_list_name,
                            currency: item[0].currency,
                            value: item[0].price,
                            items: [item]
                        }});
                    }});
                }});";
        }

        public async Task<string> GetCheckoutScriptAsync(bool addPaymentInfo = false, bool addShippingInfo = false)
        {
            var currency = _workContext.WorkingCurrency;
            var customer = _workContext.CurrentCustomer;
            var storeId = _storeContext.CurrentStore.Id;

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            var model = await cart.MapAsync();
            var productsScript = GetShoppingCartItemsScript(model.Items.ToList());

            addPaymentInfo = addPaymentInfo && model.OrderReviewData.PaymentMethod.HasValue();
            addShippingInfo = addShippingInfo && model.OrderReviewData.ShippingMethod.HasValue();

            return @$"
                // Begin checkout script
                let eventDataCart = {productsScript}

                window.eventDataStore.push(eventDataCart);

                gtag('event', 'begin_checkout', {{
                    currency: '{currency}',
                    value: {subTotalConverted.Amount.ToStringInvariant()},
                    coupon: '{model.DiscountBox.CurrentCode}',
                    {(addPaymentInfo ? $"payment_type: '{model.OrderReviewData.PaymentMethod}'," : string.Empty)}
                    {(addShippingInfo ? $"shipping_tier: '{model.OrderReviewData.ShippingMethod}'," : string.Empty)}
                    items: eventDataCart
                }});
            ";
        }

        /// <summary>
        /// TODO: (mh) (core) Docs
        /// </summary>
        /// <param name="products"></param>
        /// <param name="listName">List identifier e.g. category-list, recently viewed products, etc.</param>
        /// <returns></returns>
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
                    product.Discount.Amount.ToStringInvariant(),
                    string.Empty,
                    product.UnitPrice.Amount.ToStringInvariant(),
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
            // TODO: (mh) (core) Create js framework for this.
            return @$"
                let eventData{listName} = {{
                    item_list_name: '{listName}',
                    {await GetItemsScriptAsync(products, listName, categoryId)}
                }}

                window.eventDataStore.push(eventData{listName});
                gtag('event', 'view_item_list', eventData{listName});

                // Item selected
                $(function () {{
                    $('.artlist').on('click', '.art-picture, .art-name > a, .add-to-cart-button, .add-to-wishlist-button, .product-details-button', function (e) {{
                        
                        // TODO: (mh) (core) For testing only. Remove when testing is done!
                        //e.preventDefault();
                        
                        var $el = $(e.target);
                        var eventType = getAnalyticsEventType($el.closest('.btn') || $el);
                        var id = $el.closest('.art').data('id');

                        // Get list from data store
                        let list = window.eventDataStore.filter(function(obj) {{
                            // TODO: (mh) (core) Get real list name
	                        return obj.item_list_name === 'RecentlyViewedProducts';
                        }});

                        if (list[0]){{
                            let item = list[0].items.filter(function(obj) {{
	                            return obj.entity_id === id;
                            }});

                            // Fire event
                            gtag('event', eventType, {{
                              item_list_name: item[0].item_list_name,
                              currency: item[0].currency,
                              value: item[0].price,
                              items: [item]
                            }});
                        }}
                    }});
                }});
                
                function getAnalyticsEventType($el) {{
                    var eventType = 'select_item';

                    if ($el.hasClass('add-to-cart-button')){{
                        eventType = 'add_to_cart';
                    }}
                    else if ($el.hasClass('add-to-wishlist-button')){{
                        eventType = 'add_to_wishlist';
                    }}

                    return eventType;
                }}
            ";
        }

        public string GetSearchTermScript(string searchTerm)
        {
            return @$"
                gtag('event', 'search', {{
                  search_term: '{searchTerm}'
                }});
            ";
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

            return $"items: [{productsScript}]";
        }

        public string GetItemScript(
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
            return @$"{{
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

                ecScript = ecScript.Replace("{DETAILS}", sb.ToString());
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
            ParseScript(script, writer, tokens);
            return writer.ToString();
        }

        private static void ParseScript(string input, TextWriter writer, IDictionary<string, Func<string>> tokens)
        {
            var replacedScript = _rgScript.Replace(input, match =>
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