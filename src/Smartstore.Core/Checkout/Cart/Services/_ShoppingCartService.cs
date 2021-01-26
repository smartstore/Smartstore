using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data.Batching;
using Smartstore.Events;
using StackExchange.Profiling.Internal;

// TODO: (ms) (core) needs multiple services and refactoring (wip)
namespace Smartstore.Core.Checkout.Cart
{
    public partial class ShoppingCartService : IShoppingCartService
    {
        // 0 = CustomerId, 1 = CartType, 2 = StoreId
        const string CartItemsKey = "sm.cartitems-{0}-{1}-{2}";
        const string CartItemsPatternKey = "sm.cartitems-*";

        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICurrencyService _currencyService;
        private readonly IProductService _productService;
        private readonly ILocalizationService _localizationService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICustomerService _customerService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPermissionService _permissionService;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IDownloadService _downloadService;
        private readonly CatalogSettings _catalogSettings;
        //private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IRequestCache _requestCache;

        public ShoppingCartService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICurrencyService currencyService,
            IProductService productService,
            ILocalizationService localizationService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IProductAttributeService productAttributeService,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IPriceFormatter priceFormatter,
            ICustomerService customerService,
            ShoppingCartSettings shoppingCartSettings,
            IEventPublisher eventPublisher,
            IPermissionService permissionService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IGenericAttributeService genericAttributeService,
            IDownloadService downloadService,
            CatalogSettings catalogSettings,
            //IOrderTotalCalculationService orderTotalCalculationService,
            IRequestCache requestCache)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _currencyService = currencyService;
            _productService = productService;
            _localizationService = localizationService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productAttributeService = productAttributeService;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _priceFormatter = priceFormatter;
            _customerService = customerService;
            _shoppingCartSettings = shoppingCartSettings;
            _eventPublisher = eventPublisher;
            _permissionService = permissionService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _genericAttributeService = genericAttributeService;
            _downloadService = downloadService;
            _catalogSettings = catalogSettings;
            //_orderTotalCalculationService = orderTotalCalculationService;
            _requestCache = requestCache;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        // TODO: (ms) (core) Find other methods where navigation properties are (maybe) accessed > check for IsLoaded and use instead of db call
        // TODO: (ms) (core) Make sure Async methods (check calls of methods in services) are always awaited (if they return something other than void)
        public virtual Task<int> CountCartItemsAsync(Customer customer, ShoppingCartType cartType, int storeId = 0)
        {
            Guard.NotNull(customer, nameof(customer));

            if (_db.IsCollectionLoaded(customer, x => x.ShoppingCartItems))
            {
                return Task.FromResult(customer.ShoppingCartItems.Sum(x => x.Quantity));
            }

            return _db.ShoppingCartItems
                .ApplyStandardFilter(cartType, storeId, customer)
                .Where(x => x.ParentItemId == null)
                .SumAsync(x => (int?)x.Quantity ?? 0);
        }

        public virtual async Task<List<OrganizedShoppingCartItem>> GetCartItemsAsync(Customer customer, ShoppingCartType cartType, int storeId = 0)
        {
            Guard.NotNull(customer, nameof(customer));

            var cacheKey = CartItemsKey.FormatInvariant(customer.Id, (int)cartType, storeId);
            var result = await _requestCache.Get(cacheKey, async () =>
            {
                var cartItems = new List<ShoppingCartItem>();
                if (_db.IsCollectionLoaded(customer, x => x.ShoppingCartItems))
                {
                    cartItems = customer.ShoppingCartItems.Where(x => x.CustomerId == customer.Id && x.ShoppingCartTypeId == (int)cartType).ToList();

                    if (storeId > 0)
                    {
                        cartItems = cartItems.Where(x => x.StoreId == storeId).ToList();
                    }
                }
                else
                {
                    cartItems = await _db.ShoppingCartItems
                         .Include(x => x.Product)
                             .ThenInclude(x => x.ProductVariantAttributes)
                         .ApplyStandardFilter(cartType, storeId, customer)
                         .ToListAsync();
                }

                // Prefetch alls product variant attributes
                var allAttributes = new ProductVariantAttributeSelection(string.Empty);

                foreach (var cartItem in cartItems)
                {
                    var attributes = new ProductVariantAttributeSelection(cartItem.RawAttributes);
                    foreach (var attribute in attributes.AttributesMap)
                    {
                        allAttributes.AddAttribute(attribute.Key, attribute.Value);
                    }
                }

                await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(allAttributes);

                return await OrganizeCartItemsAsync(cartItems);
            });

            return result;
        }

        protected virtual async Task<List<OrganizedShoppingCartItem>> OrganizeCartItemsAsync(ICollection<ShoppingCartItem> cart)
        {
            if (cart.IsNullOrEmpty())
                return new();

            var result = new List<OrganizedShoppingCartItem>();
            var parents = cart.Where(x => x.ParentItemId is null);

            foreach (var parent in parents)
            {
                var parentItem = new OrganizedShoppingCartItem(parent);

                var children = cart.Where(x => x.ParentItemId != null
                        && x.ParentItemId == parent.Id
                        && x.Id != parent.Id
                        && x.ShoppingCartTypeId == parent.ShoppingCartTypeId
                        && x.Product.CanBeBundleItem());

                // TODO: (ms) (core) Reduce database roundtrips in OrganizeCartItemsAsync
                foreach (var child in children)
                {
                    var childItem = new OrganizedShoppingCartItem(child);

                    if (child.RawAttributes.HasValue()
                        && (parent.Product?.BundlePerItemPricing ?? false)
                        && child.BundleItem != null)
                    {
                        var selection = new ProductVariantAttributeSelection(child.RawAttributes);

                        child.Product.MergeWithCombination(
                            await _productAttributeMaterializer.FindAttributeCombinationAsync(child.ProductId, selection));

                        var attributeValues = await _productAttributeMaterializer
                            .MaterializeProductVariantAttributeValuesAsync(selection);

                        if (!attributeValues.IsNullOrEmpty())
                        {
                            childItem.BundleItemData.AdditionalCharge = attributeValues.Sum(x => x.PriceAdjustment);
                        }
                    }

                    parentItem.ChildItems.Add(childItem);
                }

                result.Add(parentItem);
            }

            return result;
        }

        public virtual async void DeleteCartItemAsync(
            ShoppingCartItem shoppingCartItem,
            bool resetCheckoutData = true,
            bool removeInvalidCheckoutAttributes = false,
            bool deleteChildCartItems = true)
        {
            Guard.NotNull(shoppingCartItem, nameof(shoppingCartItem));

            var customer = shoppingCartItem.Customer;
            var storeId = shoppingCartItem.StoreId;
            var cartItemId = shoppingCartItem.Id;

            if (resetCheckoutData && customer != null)
            {
                // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
                //_customerService.ResetCheckoutData(shoppingCartItem.Customer, shoppingCartItem.StoreId);
            }

            _db.Remove(shoppingCartItem);
            _requestCache.RemoveByPattern(CartItemsPatternKey);

            // Validate checkout attributes, removes attributes that require shipping
            if (removeInvalidCheckoutAttributes && shoppingCartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && customer != null)
            {
                var rawCheckoutAttributes = customer.GenericAttributes.CheckoutAttributes;
                var selection = new CheckoutAttributeSelection(rawCheckoutAttributes);
                var attributes = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(selection);
                var attributeIdsToRemove = attributes.GetInvalidShippableAttributesIds(
                    await GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, storeId));

                selection.RemoveAttributes(attributeIdsToRemove);
                customer.GenericAttributes.CheckoutAttributes = selection.AsJson();
            }

            // Delete child items
            if (deleteChildCartItems && customer != null)
            {
                var childCartItems = _db.ShoppingCartItems
                    .Where(x => x.CustomerId == customer.Id
                        && x.ParentItemId != null
                        && x.ParentItemId.Value == cartItemId
                        && x.Id != cartItemId);

                // TODO: (ms) (core) Add batch delete/remove
                foreach (var cartItem in childCartItems)
                {
                    DeleteCartItemAsync(cartItem, resetCheckoutData, removeInvalidCheckoutAttributes, false);
                }
            }
        }

        public virtual async Task<int> DeleteExpiredCartItemsAsync(DateTime olderThanUtc, int? customerId = null)
        {
            var query = _db.ShoppingCartItems
                .Where(x => x.UpdatedOnUtc < olderThanUtc && x.ParentItemId == null);

            if (customerId != null)
            {
                query = query.Where(x => x.CustomerId == customerId);
            }

            var cartItems = query.ToList();
            foreach (var parentItem in cartItems)
            {
                var childItems = await _db.ShoppingCartItems
                    .Where(x => x.ParentItemId == parentItem.Id && x.Id != parentItem.Id)
                    .BatchDeleteAsync();
            }

            await query.BatchDeleteAsync();
            return cartItems.Count;
        }

        public virtual async Task<List<string>> GetRequiredProductWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            int storeId,
            bool addRequiredProducts)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(product, nameof(product));

            var cart = await GetCartItemsAsync(customer, shoppingCartType, storeId);
            var warnings = new List<string>();

            if (product.RequireOtherProducts)
            {
                var requiredProducts = await _db.Products.GetManyAsync(product.ParseRequiredProductIds());
                var notInCart = requiredProducts.Where(x => !cart.Select(x => x.Item.ProductId).Contains(x.Id));

                foreach (var product2 in notInCart)
                {
                    if (product2.AutomaticallyAddRequiredProducts && addRequiredProducts)
                    {
                        // TODO: (ms) (core) implement AddToCart method.
                        // pass 'false' for 'automaticallyAddRequiredProducsIfEnabled' to prevent circular references
                        //var addToCartWarnings = AddToCart(customer, rp, shoppingCartType, storeId, "", decimal.Zero, 1, false, null);
                        //if (addToCartWarnings.Count > 0)
                        //{
                        //    // a product wasn't atomatically added for some reasons

                        //    // don't display specific errors from 'addToCartWarnings' variable
                        //    // display only generic error
                        //    warnings.Add(T("ShoppingCart.RequiredProductWarning", rp.GetLocalized(x => x.Name)));
                        //}
                    }
                    else
                    {
                        warnings.Add(T("ShoppingCart.RequiredProductWarning", product2.GetLocalized(x => x.Name)));
                    }
                }

            }

            return warnings;
        }

        public virtual async Task<IList<string>> GetStandardWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection,
            decimal customerEnteredPrice,
            int quantity,
            int? storeId = null)
        {
            Guard.NotNull(selection, nameof(selection));
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(product, nameof(product));

            storeId ??= _storeContext.CurrentStore.Id;

            var warnings = new List<string>();

            if (product.Deleted)
            {
                warnings.Add(T("ShoppingCart.ProductDeleted"));
                return warnings;
            }

            // Grouped products are not available for order
            if (product.ProductType == ProductType.GroupedProduct)
            {
                warnings.Add(T("ShoppingCart.ProductNotAvailableForOrder"));
            }

            // Validate product bundle, no customer entered price allowed
            if (product.ProductType == ProductType.BundledProduct
                && product.BundlePerItemPricing
                && customerEnteredPrice != decimal.Zero)
            {
                warnings.Add(T("ShoppingCart.Bundle.NoCustomerEnteredPrice"));
            }

            if (!product.Published)
            {
                warnings.Add(T("ShoppingCart.ProductUnpublished"));
            }

            if (!await _aclService.AuthorizeAsync(product, customer))
            {
                warnings.Add(T("ShoppingCart.ProductUnpublished"));
            }

            if (!await _storeMappingService.AuthorizeAsync(product.Name, product.Id, storeId.Value))
            {
                warnings.Add(T("ShoppingCart.ProductUnpublished"));
            }

            // Disabled buy button
            if (shoppingCartType == ShoppingCartType.ShoppingCart && product.DisableBuyButton)
            {
                warnings.Add(T("ShoppingCart.BuyingDisabled"));
            }

            // Disabled wishlist button
            if (shoppingCartType == ShoppingCartType.Wishlist && product.DisableWishlistButton)
            {
                warnings.Add(T("ShoppingCart.WishlistDisabled"));
            }

            // Call for price
            if (shoppingCartType == ShoppingCartType.ShoppingCart && product.CallForPrice)
            {
                warnings.Add(T("Products.CallForPrice"));
            }

            // Customer entered price            
            if (product.CustomerEntersPrice
                && (customerEnteredPrice < product.MinimumCustomerEnteredPrice
                || customerEnteredPrice > product.MaximumCustomerEnteredPrice))
            {
                var minimum = _currencyService.ConvertFromPrimaryStoreCurrency(product.MinimumCustomerEnteredPrice, _workContext.WorkingCurrency);
                var maximum = _currencyService.ConvertFromPrimaryStoreCurrency(product.MaximumCustomerEnteredPrice, _workContext.WorkingCurrency);

                warnings.Add(T(
                    "ShoppingCart.CustomerEnteredPrice.RangeError",
                    _priceFormatter.FormatPrice(minimum, true, displayTax: false),
                    _priceFormatter.FormatPrice(maximum, true, displayTax: false))
                    );
            }

            // Quantity validation
            var hasQtyWarnings = false;
            if (quantity < product.OrderMinimumQuantity)
            {
                warnings.Add(T("ShoppingCart.MinimumQuantity", product.OrderMinimumQuantity));
                hasQtyWarnings = true;
            }

            if (quantity > product.OrderMaximumQuantity)
            {
                warnings.Add(T("ShoppingCart.MaximumQuantity", product.OrderMaximumQuantity));
                hasQtyWarnings = true;
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(quantity))
            {
                warnings.Add(T("ShoppingCart.AllowedQuantities", string.Join(", ", allowedQuantities)));
            }

            // Checks whether out of stock validation is required.
            // a) Checks if cart type is shopping cart ||
            // b) Check whether it is allowed to add out of stock items to the whishlist
            // Otherwise out of stock validation is required
            var validateOutOfStock = shoppingCartType == ShoppingCartType.ShoppingCart || !_shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
            if (validateOutOfStock && !hasQtyWarnings)
            {
                switch (product.ManageInventoryMethod)
                {
                    case ManageInventoryMethod.ManageStock:
                        {
                            if (product.BackorderMode != BackorderMode.NoBackorders || product.StockQuantity >= quantity)
                                break;

                            var warning = product.StockQuantity > 0
                                ? T("ShoppingCart.QuantityExceedsStock", product.StockQuantity)
                                : T("ShoppingCart.OutOfStock");

                            warnings.Add(warning);
                            break;
                        }
                    case ManageInventoryMethod.ManageStockByAttributes:
                        {
                            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, selection);
                            if (combination == null || combination.AllowOutOfStockOrders || combination.StockQuantity >= quantity)
                                break;

                            var warning = combination.StockQuantity > 0
                                ? T("ShoppingCart.QuantityExceedsStock", combination.StockQuantity)
                                : T("ShoppingCart.OutOfStock");

                            warnings.Add(warning);
                            break;
                        }
                    case ManageInventoryMethod.DontManageStock:
                    default:
                        break;
                }
            }

            // Availability dates
            var availableStartDateError = false;
            if (product.AvailableStartDateTimeUtc.HasValue)
            {
                var availableStartDate = DateTime.SpecifyKind(product.AvailableStartDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableStartDate.CompareTo(DateTime.UtcNow) > 0)
                {
                    warnings.Add(T("ShoppingCart.NotAvailable"));
                    availableStartDateError = true;
                }
            }

            if (product.AvailableEndDateTimeUtc.HasValue && !availableStartDateError)
            {
                var availableEndDate = DateTime.SpecifyKind(product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableEndDate.CompareTo(DateTime.UtcNow) < 0)
                {
                    warnings.Add(T("ShoppingCart.NotAvailable"));
                }
            }

            return warnings;
        }

        public virtual async Task<IList<string>> GetShoppingCartItemAttributeWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection,
            int quantity = 1,
            ProductBundleItem bundleItem = null,
            ProductVariantAttributeCombination combination = null)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(selection, nameof(selection));

            var warnings = new List<string>();

            // customer cannot select anything... selectedAttribute is always empty
            // customer cannot select anything cause bundles have no attributes
            if (product.ProductType == ProductType.BundledProduct
                || bundleItem != null && !bundleItem.BundleProduct.BundlePerItemPricing)
                return warnings;

            // Get selected product variant attributes
            var selectedAttributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(selection);
            foreach (var attribute in selectedAttributes)
            {
                if (attribute.Product == null || attribute.Product.Id != product.Id)
                {
                    warnings.Add(T("ShoppingCart.AttributeError"));
                    return warnings;
                }
            }

            // Get existing product variant attributes
            foreach (var existingAttribute in product.ProductVariantAttributes)
            {
                if (!existingAttribute.IsRequired)
                    continue;

                var found = false;
                // Selected product attributes
                foreach (var selectedAttribute in selectedAttributes)
                {
                    if (selectedAttribute.Id == existingAttribute.Id)
                    {
                        var values = selection.GetAttributeValues(selectedAttribute.Id).Select(x => x.ToString()).ToList();
                        found = values.Find(x => !x.IsNullOrWhiteSpace()).HasValue();
                    }
                }

                // Attribute is filtered out on bundle item level... it cannot be selected by the customer
                found = !found && (bundleItem?.FilterAttributes ?? false)
                    ? !bundleItem.AttributeFilters.Any(x => x.AttributeId == existingAttribute.ProductAttributeId)
                    : true;

                if (!found)
                {
                    warnings.Add(T(
                        "ShoppingCart.SelectAttribute",
                        existingAttribute.TextPrompt.IsEmpty()
                            ? existingAttribute.ProductAttribute.GetLocalized(a => a.Name)
                            : existingAttribute.TextPrompt)
                        );
                }
            }

            // Check whether there is an active selected attribute combination
            if (warnings.Count == 0)
            {
                if (selection.AttributesMap.Any())
                {
                    if (combination == null)
                    {
                        combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, selection);
                    }

                    if (combination != null && !combination.IsActive)
                    {
                        warnings.Add(T("ShoppingCart.NotAvailable"));
                    }
                }

                var values = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(selection);
                var linkedProductIds = values.Select(x => x.LinkedProductId);
                var linkedProducts = await _db.Products.GetManyAsync(linkedProductIds);

                foreach (var value in values)
                {
                    if (value.ValueType != ProductVariantAttributeValueType.ProductLinkage)
                        continue;

                    var linkedProduct = linkedProducts.Find(x => x.Id == value.LinkedProductId);
                    if (linkedProduct == null)
                    {
                        warnings.Add(T("ShoppingCart.ProductLinkageProductNotLoading", value.LinkedProductId));
                        continue;
                    }

                    var linkageWarnings = await GetShoppingCartItemWarningsAsync(
                        customer,
                        shoppingCartType,
                        linkedProduct,
                        _storeContext.CurrentStore.Id,
                        selection,
                        decimal.Zero,
                        quantity * value.Quantity,
                        false,
                        true,
                        true,
                        true,
                        true);

                    foreach (var linkageWarning in linkageWarnings)
                    {
                        warnings.Add(T(
                            "ShoppingCart.ProductLinkageAttributeWarning",
                            value.ProductVariantAttribute.ProductAttribute.GetLocalized(a => a.Name),
                            value.GetLocalized(a => a.Name),
                            linkageWarning)
                            );
                    }
                }
            }

            return warnings;
        }

        // Propably not needed since this method is not called in classic at all!
        //public virtual async Task<bool> AreAllAttributesForCombinationSelectedAsync(string selectedAttributes, Product product)
        //{
        //    Guard.NotNull(product, nameof(product));

        //    var hasAttributeCombinations = await _db.ShoppingCartItems
        //        .Include(x => x.Product)
        //            .ThenInclude(x => x.ProductVariantAttributeCombinations)
        //        .Select(x => x.Product.ProductVariantAttributeCombinations)
        //        .AnyAsync();

        //    if (!hasAttributeCombinations)
        //        return true;

        //    // selected attributes
        //    var pva1Collection = _productAttributeMaterializer.ParseProductVariantAttributes(selectedAttributes);

        //    // existing product attributes
        //    var pva2Collection = product.ProductVariantAttributes;
        //    foreach (var pva2 in pva2Collection)
        //    {
        //        if (pva2.IsRequired)
        //        {
        //            bool found = false;
        //            // selected product attributes
        //            foreach (var pva1 in pva1Collection)
        //            {
        //                if (pva1.Id == pva2.Id)
        //                {
        //                    var pvaValuesStr = _productAttributeMaterializer.ParseValues(selectedAttributes, pva1.Id);
        //                    foreach (string str1 in pvaValuesStr)
        //                    {
        //                        if (!String.IsNullOrEmpty(str1.Trim()))
        //                        {
        //                            found = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //            }

        //            if (!found)
        //            {
        //                return found;
        //            }
        //        }
        //        else
        //        {
        //            return true;
        //        }
        //    }

        //    return true;
        //}

        public virtual IList<string> GetShoppingCartItemGiftCardWarnings(
            Product product,
            ShoppingCartType shoppingCartType,
            ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(selection, nameof(selection));

            var warnings = new List<string>();

            if (!product.IsGiftCard)
                return warnings;

            var recipientName = selection.GiftCardInfo?.RecipientName;
            var recipientEmail = selection.GiftCardInfo?.RecipientEmail;
            var senderName = selection.GiftCardInfo?.SenderName;
            var senderEmail = selection.GiftCardInfo?.SenderEmail;

            if (recipientName.IsNullOrWhiteSpace())
            {
                warnings.Add(T("ShoppingCart.RecipientNameError"));
            }

            if (senderName.IsNullOrWhiteSpace())
            {
                warnings.Add(T("ShoppingCart.SenderNameError"));
            }

            if (product.GiftCardType == GiftCardType.Virtual)
            {
                if (recipientEmail.IsNullOrWhiteSpace() || !recipientEmail.IsEmail())
                {
                    warnings.Add(T("ShoppingCart.RecipientEmailError"));
                }

                if (senderEmail.IsNullOrWhiteSpace() || !senderEmail.IsEmail())
                {
                    warnings.Add(T("ShoppingCart.SenderEmailError"));
                }
            }

            return warnings;
        }

        public virtual IList<string> GetBundleItemWarnings(ProductBundleItem bundleItem)
        {
            var warnings = new List<string>();

            if (bundleItem == null)
                return warnings;

            var name = bundleItem.GetLocalizedName();
            if (!bundleItem.Published)
            {
                warnings.Add(T("ShoppingCart.Bundle.BundleItemUnpublished", name));
            }

            if (bundleItem.ProductId == 0
                || bundleItem.Product == null
                || bundleItem.BundleProductId == 0
                || bundleItem.BundleProduct == null)
            {
                warnings.Add(T("ShoppingCart.Bundle.MissingProduct", name));
            }

            if (bundleItem.Quantity <= 0)
            {
                warnings.Add(T("ShoppingCart.Bundle.Quantity", name));
            }

            if (bundleItem.Product.IsDownload || bundleItem.Product.IsRecurring)
            {
                warnings.Add(T("ShoppingCart.Bundle.ProductResrictions", name));
            }

            return warnings;
        }

        public virtual IList<string> GetCartBundleItemWarnings(IList<OrganizedShoppingCartItem> cartItems)
        {
            var warnings = new List<string>();

            if (cartItems.IsNullOrEmpty())
                return warnings;

            var bundleItems = cartItems
                .Where(x => x.Item.BundleItem != null)
                .Select(x => x.Item.BundleItem);

            foreach (var bundleItem in bundleItems)
            {
                warnings.AddRange(GetBundleItemWarnings(bundleItem));
            }

            return warnings;
        }

        public virtual async Task<IList<string>> GetShoppingCartItemWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            int storeId,
            ProductVariantAttributeSelection selection,
            decimal customerEnteredPrice,
            int quantity,
            bool addRequiredProducts,
            bool getStandardWarnings = true,
            bool getAttributesWarnings = true,
            bool getGiftCardWarnings = true,
            bool getRequiredProductWarnings = true,
            bool getBundleWarnings = true,
            ProductBundleItem bundleItem = null,
            IList<OrganizedShoppingCartItem> childItems = null)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(selection, nameof(selection));

            var warnings = new List<string>();

            if (getStandardWarnings)
                warnings.AddRange(await GetStandardWarningsAsync(customer, shoppingCartType, product, selection, customerEnteredPrice, quantity, storeId));

            if (getAttributesWarnings)
                warnings.AddRange(await GetShoppingCartItemAttributeWarningsAsync(customer, shoppingCartType, product, selection, quantity, bundleItem));

            if (getGiftCardWarnings)
                warnings.AddRange(GetShoppingCartItemGiftCardWarnings(product, shoppingCartType, selection));

            if (getRequiredProductWarnings)
                warnings.AddRange(await GetRequiredProductWarningsAsync(customer, shoppingCartType, product, storeId, addRequiredProducts));

            // Bundle and bundle item warnings
            if (getBundleWarnings)
            {
                if (bundleItem != null)
                {
                    warnings.AddRange(GetBundleItemWarnings(bundleItem));
                }

                if (childItems != null)
                {
                    warnings.AddRange(GetCartBundleItemWarnings(childItems));
                }
            }

            return warnings;
        }

        public virtual async Task<IList<string>> GetShoppingCartWarningsAsync(
            IList<OrganizedShoppingCartItem> shoppingCart,
            CheckoutAttributeSelection selection, 
            bool validateCheckoutAttributes)
        {
            var warnings = new List<string>();

            var missingProduct = shoppingCart.Where(x => x.Item.Product is null).FirstOrDefault();
            if (missingProduct != null)
            {
                warnings.Add(T("ShoppingCart.CannotLoadProduct", missingProduct.Item.ProductId));
                return warnings;
            }

            var hasStandardProducts = shoppingCart.IncludesStandardProducts();
            var hasRecurringProducts = shoppingCart.IncludesRecurringProducts();
            // Do not mix standard and recurring products
            if (hasStandardProducts && hasRecurringProducts)
            {
                warnings.Add(T("ShoppingCart.CannotMixStandardAndAutoshipProducts"));
            }

            // Validate recurring products
            if (hasRecurringProducts)
            {
                var recurringCycleInfo = shoppingCart.GetRecurringCycleInfo(_localizationService);
                if (!recurringCycleInfo.ErrorMessage.IsNullOrWhiteSpace())
                {
                    warnings.Add(recurringCycleInfo.ErrorMessage);
                    return warnings;
                }
            }

            if (validateCheckoutAttributes)
            {
                var existingAttributesQuery = _db.CheckoutAttributes.ApplyStoreFilter(_storeContext.CurrentStore.Id);
                if (!shoppingCart.IsShippingRequired())
                {
                    // Filter attributes which require shippable products
                    existingAttributesQuery = existingAttributesQuery.Where(x => !x.ShippableProductRequired);
                }

                var existingAttributes = await existingAttributesQuery.ToListAsync();
                var selectedAttributesValues = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(selection);

                // Check for not selected attributes
                foreach (var existingAttribute in existingAttributes)
                {
                    if (!existingAttribute.IsRequired)
                        continue;

                    var found = selectedAttributesValues
                        .Where(x => x.CheckoutAttributeId == existingAttribute.Id)
                        .Any();

                    if (found)
                        continue;

                    string textPrompt = existingAttribute.GetLocalized(a => a.TextPrompt);
                    if (textPrompt.HasValue())
                    {
                        warnings.Add(textPrompt);
                    }
                    else
                    {
                        warnings.Add(T("ShoppingCart.SelectAttribute", existingAttribute.GetLocalized(a => a.Name)));
                    }
                }
            }

            return warnings;
        }

        // Has become an extension method
        //public virtual OrganizedShoppingCartItem FindShoppingCartItemInTheCart(
        //    IList<OrganizedShoppingCartItem> shoppingCart,
        //    ShoppingCartType shoppingCartType,
        //    Product product,
        //    ProductVariantAttributeSelection selection,
        //    decimal customerEnteredPrice = decimal.Zero

        //    )
        //{
        //    Guard.NotNull(shoppingCart, nameof(shoppingCart));
        //    Guard.NotNull(product, nameof(product));

        //    // Return on product bundle with individual item pricing - too complex
        //    if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
        //        return null;

        //    // Filter non group items from correct cart type, with matching product id and product type id
        //    var cart = shoppingCart
        //        .Where(x => x.Item.ShoppingCartType == shoppingCartType
        //        && x.Item.ParentItemId == null
        //        && x.Item.Product.ProductTypeId == product.ProductTypeId
        //        && x.Item.ProductId == product.Id);

        //    // There could be multiple matching products with the same identifier but different attributes/selections (etc).
        //    // Ensure matching product infos are the same (attributes, gift card values (if it is one), customerEnteredPrice).
        //    foreach (var cartItem in cart)
        //    {
        //        // Compare attribute selection
        //        var cartItemSelection = new ProductVariantAttributeSelection(cartItem.Item.RawAttributes);                
        //        if (cartItemSelection != selection)
        //            continue;

        //        var currentProduct = cartItem.Item.Product;

        //        // Compare gift cards info values (if it is a gift card)
        //        if (currentProduct.IsGiftCard &&
        //            (cartItemSelection.GiftCardInfo == null
        //            || selection.GiftCardInfo == null
        //            || cartItemSelection != selection))
        //        {
        //            continue;
        //        }

        //        // Products with CustomerEntersPrice are equal if the price is the same.
        //        // But a system product may only be placed once in the shopping cart.
        //        if (currentProduct.CustomerEntersPrice && !currentProduct.IsSystemProduct
        //            && Math.Round(cartItem.Item.CustomerEnteredPrice, 2) != Math.Round(customerEnteredPrice, 2))
        //        {
        //            continue;
        //        }

        //        // If we got this far, we found a matching product with the same values
        //        return cartItem;
        //    }

        //    return null;
        //}

        public virtual async Task<List<string>> AddToCartAsync(
            Customer customer,
            Product product,
            ShoppingCartType cartType,
            int storeId,
            ProductVariantAttributeSelection selection,
            decimal customerEnteredPrice,
            int quantity,
            bool addRequiredProducts,
            AddToCartContext ctx = null)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(product, nameof(product));

            var warnings = new List<string>();

            // When context contains a bundle item, check for bundle errors
            var bundleItem = ctx?.BundleItem;
            if (bundleItem != null && ctx.Warnings.Count > 0)
                return ctx.Warnings;

            if (cartType == ShoppingCartType.ShoppingCart && !await _permissionService.AuthorizeAsync(Permissions.Cart.AccessShoppingCart, customer))
            {
                warnings.Add(T("ShoppingCart.IsDisabled"));
                return warnings;
            }

            if (cartType == ShoppingCartType.Wishlist && !await _permissionService.AuthorizeAsync(Permissions.Cart.AccessWishlist, customer))
            {
                warnings.Add(T("Wishlist.IsDisabled"));
                return warnings;
            }

            if (quantity <= 0)
            {
                warnings.Add(T("ShoppingCart.QuantityShouldPositive"));
                return warnings;
            }

            // TODO: (ms) (core) This code has been commented out before in smartstore classic, is it needed at all?
            //if (parentItemId.HasValue && (parentItemId.Value == 0 || bundleItem == null || bundleItem.Id == 0))
            //{
            //	warnings.Add(T("ShoppingCart.Bundle.BundleItemNotFound", bundleItem.GetLocalizedName()));
            //	return warnings;
            //}

            // reset checkout info
            // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
            //_customerService.ResetCheckoutData(customer, storeId);

            var cart = await GetCartItemsAsync(customer, cartType, storeId);
            OrganizedShoppingCartItem existingCartItem = null;

            if (bundleItem == null)
            {
                existingCartItem = cart.FindItemInCart(cartType, product, selection, customerEnteredPrice);
            }

            if (existingCartItem != null)
            {
                // Product is already in cart, update existing item
                var newQuantity = existingCartItem.Item.Quantity + quantity;
                warnings.AddRange(
                    await GetShoppingCartItemWarningsAsync(
                        customer,
                        cartType,
                        product,
                        storeId,
                        selection,
                        customerEnteredPrice,
                        newQuantity,
                        addRequiredProducts,
                        bundleItem: bundleItem)
                    );

                if (warnings.Count == 0)
                {
                    existingCartItem.Item.RawAttributes = selection.AsJson();
                    existingCartItem.Item.Quantity = newQuantity;
                    existingCartItem.Item.UpdatedOnUtc = DateTime.UtcNow;
                    _db.Customers.Update(customer);
                }
            }
            else
            {
                // Product is not in shopping cart yet, create new item
                warnings.AddRange(
                    await GetShoppingCartItemWarningsAsync(
                        customer,
                        cartType,
                        product,
                        storeId,
                        selection,
                        customerEnteredPrice,
                        quantity,
                        addRequiredProducts,
                        bundleItem: bundleItem)
                );

                if (warnings.Count == 0)
                {
                    // Item maximum quantity validation
                    if (cartType == ShoppingCartType.ShoppingCart && cart.Count >= _shoppingCartSettings.MaximumShoppingCartItems)
                    {
                        warnings.Add(T("ShoppingCart.MaximumShoppingCartItems"));
                        return warnings;
                    }
                    else if (cartType == ShoppingCartType.Wishlist && cart.Count >= _shoppingCartSettings.MaximumWishlistItems)
                    {
                        warnings.Add(T("ShoppingCart.MaximumWishlistItems"));
                        return warnings;
                    }

                    var cartItem = new ShoppingCartItem
                    {
                        ShoppingCartType = cartType,
                        StoreId = storeId,
                        Product = product,
                        RawAttributes = selection.AsJson(),
                        CustomerEnteredPrice = customerEnteredPrice,
                        Quantity = quantity,
                        ParentItemId = null
                    };

                    if (bundleItem != null)
                    {
                        cartItem.BundleItemId = bundleItem.Id;
                    }

                    if (ctx == null)
                    {
                        customer.ShoppingCartItems.Add(cartItem);
                        _db.Customers.Update(customer);
                    }
                    else
                    {
                        if (bundleItem == null)
                        {
                            Debug.Assert(ctx.Item == null, "Add to cart item already specified");
                            ctx.Item = cartItem;
                        }
                        else
                        {
                            ctx.ChildItems.Add(cartItem);
                        }
                    }
                }
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return warnings;
        }

        // TODO: (ms) (core) Several todos in this methods (ProductVariantQuery, ResetCheckoutData(), GetGiftcard by product etc)
        //public virtual async void AddToCartAsync(AddToCartContext ctx)
        //{
        //    var customer = ctx.Customer ?? _workContext.CurrentCustomer;
        //    var storeId = ctx.StoreId ?? _storeContext.CurrentStore.Id;

        //    // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
        //    // _customerService.ResetCheckoutData(customer, storeId);

        //    // TODO: (ms) (core) Implement ProductVariantQuery
        //    if (ctx.VariantQuery != null)
        //    {
        //        //var attributes = _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(ctx.AttributeSelection);

        //        ////var attributes = await _db.ProductVariantAttributes
        //        ////    .Include(x => x.ProductAttribute)
        //        ////    .Where(x => x.ProductId == ctx.Product.Id)
        //        ////    .ToListAsync();

        //        //ctx.RawAttributes = ctx.VariantQuery.CreateSelectedAttributesXml(ctx.Product.Id, ctx.BundleItemId, attributes, _productAttributeMaterializer,
        //        //    _localizationService, _downloadService, _catalogSettings, null, ctx.Warnings);

        //        if (ctx.Product.ProductType == ProductType.BundledProduct && ctx.AttributeSelection.AttributesMap.Any())
        //        {
        //            ctx.Warnings.Add(T("ShoppingCart.Bundle.NoAttributes"));
        //        }

        //        if (ctx.Product.IsGiftCard)
        //        {
        //            // TODO (ms) (core) implement product variant query to get gift card from product

        //            //var giftCard = ctx.VariantQuery.GetGiftCard()

        //            //ctx.Attributes = ctx.AttributeForm.AddGiftCardAttribute(ctx.Attributes, ctx.Product.Id, _productAttributeParser, ctx.BundleItemId);
        //            ctx.AttributeSelection.GiftCardInfo = new GiftCardInfo
        //            {
        //                RecipientName = "",
        //                RecipientEmail = "",
        //                SenderName = "",
        //                SenderEmail = "",
        //                Message = ""
        //            };
        //        }
        //    }

        //    ctx.Warnings.AddRange(
        //        await AddToCartAsync(
        //            customer,
        //            ctx.Product,
        //            ctx.CartType,
        //            storeId,
        //            ctx.AttributeSelection,
        //            ctx.CustomerEnteredPrice,
        //            ctx.Quantity,
        //            ctx.AddRequiredProducts,
        //            ctx)
        //        );

        //    if (ctx.Product.ProductType == ProductType.BundledProduct
        //        && ctx.Warnings.Count <= 0
        //        && ctx.BundleItem == null)
        //    {
        //        var products = _db.Products.AsQueryable();

        //        var query = _db.ProductBundleItem
        //             .Join<Product>(products, x => x.ProductId, x => x.Id, x => x)
        //             .ToList();

        //        query.Where(x => x.BundleProductId == ctx.Product.Id && (x.Published && (products.Where(y => y.Id == x.ProductId). )))

        //        var items = _productService.GetBundleItems(ctx.Product.Id).Select(x => x.Item);
        //        foreach (var bundleItem in items)
        //        {
        //            await AddToCartAsync(new AddToCartContext
        //            {
        //                BundleItem = bundleItem,
        //                Warnings = ctx.Warnings,
        //                Item = ctx.Item,
        //                ChildItems = ctx.ChildItems,
        //                Product = bundleItem.Product,
        //                Customer = customer,
        //                VariantQuery = ctx.VariantQuery,
        //                CartType = ctx.CartType,
        //                Quantity = bundleItem.Quantity,
        //                AddRequiredProducts = ctx.AddRequiredProducts,
        //                StoreId = storeId
        //            });

        //            if (ctx.Warnings.Count > 0)
        //            {
        //                ctx.ChildItems.Clear();
        //                break;
        //            }
        //        }
        //    }

        //    if (ctx.BundleItem == null)
        //    {
        //        AddToCartStoring(ctx);
        //    }
        //}

        public virtual void AddToCartStoring(AddToCartContext ctx)
        {
            if (ctx.Warnings.Count != 0 || ctx.Item == null)
                return;

            var customer = ctx.Customer ?? _workContext.CurrentCustomer;
            customer.ShoppingCartItems.Add(ctx.Item);

            if (!ctx.ChildItems.IsNullOrEmpty())
            {
                foreach (var childItem in ctx.ChildItems)
                {
                    childItem.ParentItemId = ctx.Item.Id;
                }

                customer.ShoppingCartItems.AddRange(ctx.ChildItems);
            }

            _db.TryChangeState(customer, EntityState.Modified);
            // SaveChanges
        }

        public virtual async Task<IList<string>> UpdateShoppingCartItemAsync(Customer customer, int shoppingCartItemId, int newQuantity, bool resetCheckoutData)
        {
            Guard.NotNull(customer, nameof(customer));

            var warnings = new List<string>();

            var shoppingCartItem = customer.ShoppingCartItems.FirstOrDefault(x => x.Id == shoppingCartItemId && x.ParentItemId == null);
            if (shoppingCartItem != null)
            {
                if (resetCheckoutData)
                {
                    // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
                    // _customerService.ResetCheckoutData(customer, shoppingCartItem.StoreId);
                }

                if (newQuantity > 0)
                {
                    warnings.AddRange(
                        await GetShoppingCartItemWarningsAsync(
                            customer,
                            shoppingCartItem.ShoppingCartType,
                            shoppingCartItem.Product,
                            shoppingCartItem.StoreId,
                            new(shoppingCartItem.RawAttributes),
                            shoppingCartItem.CustomerEnteredPrice,
                            newQuantity,
                            false)
                        );

                    if (warnings.Count == 0)
                    {
                        shoppingCartItem.Quantity = newQuantity;
                        shoppingCartItem.UpdatedOnUtc = DateTime.UtcNow;
                        _db.Customers.Update(customer);
                    }
                }
                else
                {
                    DeleteCartItemAsync(shoppingCartItem, resetCheckoutData, true);
                }
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return warnings;
        }

        public virtual async void MigrateShoppingCartAsync(Customer fromCustomer, Customer toCustomer)
        {
            Guard.NotNull(fromCustomer, nameof(fromCustomer));
            Guard.NotNull(toCustomer, nameof(toCustomer));

            if (fromCustomer.Id == toCustomer.Id)
                return;

            var storeId = 0;
            var cartItems = await OrganizeCartItemsAsync(fromCustomer.ShoppingCartItems);

            if (cartItems.IsNullOrEmpty())
                return;

            foreach (var cartItem in cartItems)
            {
                if (storeId == 0)
                {
                    storeId = cartItem.Item.StoreId;
                }

                // TODO: (ms) (core) Implement Copy method
                await CopyAsync(cartItem, toCustomer, cartItem.Item.ShoppingCartType, cartItem.Item.StoreId, false);
            }

            // TODO: (ms) (core) Implement publishMigrateShoppingCart
            //_eventPublisher.PublishMigrateShoppingCart(fromCustomer, toCustomer, storeId);

            foreach (var cartItem in cartItems)
            {
                // TODO: (ms) (core) Have some deleteCartItemsAsync method for batch delete
                DeleteCartItemAsync(cartItem.Item);
            }
        }

        public virtual async Task<IList<string>> CopyAsync(
            OrganizedShoppingCartItem cartItem,
            Customer customer,
            ShoppingCartType cartType,
            int storeId,
            bool addRequiredProducts)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(cartItem, nameof(cartItem));

            var addToCartContext = new AddToCartContext
            {
                Customer = customer
            };

            addToCartContext.Warnings = await AddToCartAsync(
                customer,
                cartItem.Item.Product,
                cartType,
                storeId,
                new(cartItem.Item.RawAttributes),
                cartItem.Item.CustomerEnteredPrice,
                cartItem.Item.Quantity,
                addRequiredProducts,
                addToCartContext);

            if (addToCartContext.Warnings.Count == 0 && cartItem.ChildItems != null)
            {
                foreach (var childItem in cartItem.ChildItems)
                {
                    addToCartContext.BundleItem = childItem.Item.BundleItem;

                    addToCartContext.Warnings = await AddToCartAsync(
                        customer,
                        childItem.Item.Product,
                        cartType,
                        storeId,
                        new(childItem.Item.RawAttributes),
                        childItem.Item.CustomerEnteredPrice,
                        childItem.Item.Quantity,
                        false,
                        addToCartContext);
                }
            }

            AddToCartStoring(addToCartContext);

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return addToCartContext.Warnings;
        }

        // TODO: (ms) (core) Implement orderTotalCalculationService
        //public virtual async decimal GetCurrentCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart = null)
        //{
        //    cart ??= await GetCartItemsAsync(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

        //    if (cart.Any())
        //    {
        //        _orderTotalCalculationService.GetShoppingCartSubTotal(cart, out _, out _, out var subTotalWithoutDiscountBase, out _);

        //        return _currencyService.ConvertFromPrimaryStoreCurrency(subTotalWithoutDiscountBase, _workContext.WorkingCurrency);
        //    }

        //    return Task.FromResult(decimal.Zero);
        //}

        // TODO: (ms) (core) Implement GetCurrentCartSubTotalAsync()
        //public virtual string GetFormattedCurrentCartSubTotal()
        //{
        //    return _priceFormatter.FormatPrice(GetCurrentCartSubTotalAsync());
        //}

        // TODO: (ms) (core) Implement GetCurrentCartSubTotalAsync()
        //public virtual string GetFormattedCurrentCartSubTotal(IList<OrganizedShoppingCartItem> cart)
        //{
        //    return _priceFormatter.FormatPrice(GetCurrentCartSubTotal(cart));
        //}

        public async Task<decimal> GetAllOpenCartSubTotalAsync()
        {
            var subTotal = await _db.ShoppingCartItems
                .Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart && x.Product != null)
                .SumAsync(x => (decimal?)(x.Product.Price * x.Quantity)) ?? decimal.Zero;

            return subTotal;
        }

        public async Task<decimal> GetAllOpenWishlistSubTotalAsync()
        {
            var subTotal = await _db.ShoppingCartItems
                .Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.Wishlist && x.Product != null)
                .SumAsync(x => (decimal?)(x.Product.Price * x.Quantity)) ?? decimal.Zero;

            return subTotal;
        }
    }
}
