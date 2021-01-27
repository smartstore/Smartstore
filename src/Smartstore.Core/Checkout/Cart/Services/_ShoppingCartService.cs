using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Data.Batching;

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
        private readonly IRequestCache _requestCache;
        private readonly ShoppingCartSettings _cartSettings;
        private readonly IShoppingCartValidator _cartValidator;
        private readonly IPermissionService _permissionService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        //private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        public ShoppingCartService(
            SmartDbContext db,
            IWorkContext workContext,
            IRequestCache requestCache,
            ShoppingCartSettings cartSettings,
            IShoppingCartValidator cartValidator,            
            IPermissionService permissionService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer
            //,IOrderTotalCalculationService orderTotalCalculationService,
            )
        {
            _db = db;
            _workContext = workContext;
            _requestCache = requestCache;
            _cartSettings = cartSettings;
            _cartValidator = cartValidator;
            _permissionService = permissionService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            //_orderTotalCalculationService = orderTotalCalculationService;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        // TODO: (ms) (core) Find other methods where navigation properties are (maybe) accessed > check for IsLoaded and use instead of db call
        // TODO: (ms) (core) Make sure Async methods (check calls of methods in services) are always awaited
        public virtual Task<int> CountCartItemsAsync(Customer customer, ShoppingCartType cartType, int storeId = 0)
        {
            Guard.NotNull(customer, nameof(customer));

            if (_db.IsCollectionLoaded(customer, x => x.ShoppingCartItems))
            {
                var cartItems = customer.ShoppingCartItems
                    .Where(x => x.ParentItemId == null && x.ShoppingCartTypeId == (int)cartType);

                if (customer != null)
                {
                    cartItems = cartItems.Where(x => x.CustomerId == customer.Id);
                }

                if (storeId > 0)
                {
                    cartItems = cartItems.Where(x => x.StoreId == storeId);
                }

                return Task.FromResult(cartItems.Sum(x => x.Quantity));
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
                    var filteredCartItems = customer.ShoppingCartItems
                        .Where(x => x.CustomerId == customer.Id && x.ShoppingCartTypeId == (int)cartType);
                        
                    if (storeId > 0)
                    {
                        filteredCartItems = cartItems.Where(x => x.StoreId == storeId);
                    }

                    cartItems = filteredCartItems.ToList();
                }
                else
                {
                    cartItems = await _db.ShoppingCartItems
                         .Include(x => x.Product)
                             .ThenInclude(x => x.ProductVariantAttributes)
                         .ApplyStandardFilter(cartType, storeId, customer)
                         .ToListAsync();
                }

                // Prefetch all product variant attributes
                var allAttributes = new ProductVariantAttributeSelection(string.Empty);
                var allAttributeMaps = cartItems.SelectMany(x => x.AttributeSelection.AttributesMap);

                foreach (var attribute in allAttributeMaps)
                {
                    if (allAttributes.AttributesMap.Contains(attribute))
                        continue;

                    allAttributes.AddAttribute(attribute.Key, attribute.Value);
                }

                // TODO: (ms) (core) Check if this is sufficient and good prefetch -> what about caching or skipping already loaded?
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

            // TODO: (ms) (core) to reduce db roundtrips -> load and filter children by parents (id and so on) into lists and try to get from db as batch request

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

                        await _productAttributeMaterializer.MergeWithCombinationAsync(child.Product, selection);

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
        public virtual async Task<int> DeleteCartItemAsync(
            ShoppingCartItem shoppingCartItem,
            bool resetCheckoutData = true,
            bool removeInvalidCheckoutAttributes = false,
            bool deleteChildCartItems = true)
        {
            return await DeleteCartItemsAsync(new List<ShoppingCartItem> { shoppingCartItem }, resetCheckoutData, removeInvalidCheckoutAttributes, deleteChildCartItems);
        }

        public virtual async Task<int> DeleteCartItemsAsync(
            IEnumerable<ShoppingCartItem> shoppingCartItems,
            bool resetCheckoutData = true,
            bool removeInvalidCheckoutAttributes = false,
            bool deleteChildCartItems = true)
        {
            Guard.NotNull(shoppingCartItems, nameof(shoppingCartItems));

            var customer = shoppingCartItems.Select(x => x.Customer).FirstOrDefault();
            if (resetCheckoutData && customer != null)
            {
                // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
                //_customerService.ResetCheckoutData(shoppingCartItem.Customer, shoppingCartItem.StoreId);
            }

            _db.RemoveRange(shoppingCartItems);
            _requestCache.RemoveByPattern(CartItemsPatternKey);

            var storeId = shoppingCartItems.Select(x => x.StoreId).FirstOrDefault();
            var cartItemIds = shoppingCartItems.Select(x => x.Id).ToList();
            var cartType = shoppingCartItems.Select(x => x.ShoppingCartType).FirstOrDefault();

            // Delete all child items
            if (deleteChildCartItems && customer != null)
            {
                var childCartItems = await _db.ShoppingCartItems
                    .Where(x => x.CustomerId == customer.Id
                        && x.ParentItemId != null
                        && cartItemIds.Contains(x.ParentItemId.Value)
                        && !cartItemIds.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            // Validate checkout attributes, removes attributes that require shipping (if cart does not require shipping)
            if (removeInvalidCheckoutAttributes && cartType == ShoppingCartType.ShoppingCart && customer != null)
            {
                var rawCheckoutAttributes = customer.GenericAttributes.CheckoutAttributes;
                var selection = new CheckoutAttributeSelection(rawCheckoutAttributes);
                var attributes = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(selection);
                var cartItems = await GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, storeId);
                var attributeIdsToRemove = attributes.GetInvalidShippableAttributesIds(cartItems);

                selection.RemoveAttributes(attributeIdsToRemove);
                customer.GenericAttributes.CheckoutAttributes = selection.AsJson();

                _db.TryChangeState(customer, EntityState.Modified);
            }

            return await _db.SaveChangesAsync();
        }

        public virtual async Task<int> DeleteExpiredCartItemsAsync(DateTime olderThanUtc, Customer customer)
        {
            // Gets expired (too old) cart items with no parent
            var query = _db.ShoppingCartItems
                .Where(x => x.UpdatedOnUtc < olderThanUtc && x.ParentItemId == null);

            if (customer != null)
            {
                query = query.Where(x => x.CustomerId == customer.Id);
            }

            var cartItems = await query.ToListAsync();
            var cartItemIds = cartItems.Select(x => x.Id);

            // Delete all child cart items
            await _db.ShoppingCartItems
                    .Where(x => x.ParentItemId != null
                    && cartItemIds.Contains(x.ParentItemId.Value)
                    && !cartItemIds.Contains(x.Id))
                    .BatchDeleteAsync();

            // Delete all parent cart items
            _db.ShoppingCartItems.RemoveRange(cartItems);
            return await _db.SaveChangesAsync();
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
            ProductVariantAttributeSelection selection = null,
            decimal customerEnteredPrice = decimal.Zero,
            int quantity = 1,
            bool automaticallyAddRequiredProductsIfEnabled = false,
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
            selection ??= new ProductVariantAttributeSelection(string.Empty);

            if (bundleItem == null)
            {
                existingCartItem = cart.FindItemInCart(cartType, product, selection, customerEnteredPrice);
            }

            if (existingCartItem != null)
            {
                // Product is already in cart, update existing item
                var newQuantity = existingCartItem.Item.Quantity + quantity;
                warnings.AddRange(
                    await _cartValidator.GetShoppingCartItemWarningsAsync(
                        customer,
                        cartType,
                        product,
                        storeId,
                        selection,
                        customerEnteredPrice,
                        newQuantity,
                        automaticallyAddRequiredProductsIfEnabled,
                        bundleItem: bundleItem)
                    );

                if (warnings.Count == 0)
                {
                    existingCartItem.Item.RawAttributes = selection.AsJson();
                    existingCartItem.Item.Quantity = newQuantity;
                    existingCartItem.Item.UpdatedOnUtc = DateTime.UtcNow;
                    _db.TryChangeState(customer, EntityState.Modified);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                // Product is not in shopping cart yet, create new item
                warnings.AddRange(
                    await _cartValidator.GetShoppingCartItemWarningsAsync(
                        customer,
                        cartType,
                        product,
                        storeId,
                        selection,
                        customerEnteredPrice,
                        quantity,
                        automaticallyAddRequiredProductsIfEnabled,
                        bundleItem: bundleItem)
                );

                if (warnings.Count == 0)
                {
                    // Item maximum quantity validation
                    if (cartType == ShoppingCartType.ShoppingCart && cart.Count >= _cartSettings.MaximumShoppingCartItems)
                    {
                        warnings.Add(T("ShoppingCart.MaximumShoppingCartItems"));
                        return warnings;
                    }
                    else if (cartType == ShoppingCartType.Wishlist && cart.Count >= _cartSettings.MaximumWishlistItems)
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
                        _db.TryChangeState(customer, EntityState.Modified);
                        await _db.SaveChangesAsync();
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
        //public virtual async Task AddToCartAsync(AddToCartContext ctx)
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

        public virtual async Task AddToCartStoringAsync(AddToCartContext ctx)
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
            await _db.SaveChangesAsync();
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
                        await _cartValidator.GetShoppingCartItemWarningsAsync(
                            customer,
                            shoppingCartItem.ShoppingCartType,
                            shoppingCartItem.Product,
                            shoppingCartItem.StoreId,
                            shoppingCartItem.AttributeSelection,
                            shoppingCartItem.CustomerEnteredPrice,
                            newQuantity,
                            false)
                        );

                    if (warnings.Count == 0)
                    {
                        shoppingCartItem.Quantity = newQuantity;
                        shoppingCartItem.UpdatedOnUtc = DateTime.UtcNow;
                        _db.TryChangeState(customer, EntityState.Modified);
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    await DeleteCartItemAsync(shoppingCartItem, resetCheckoutData, true);
                }
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return warnings;
        }

        public virtual async Task MigrateShoppingCartAsync(Customer fromCustomer, Customer toCustomer)
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
                await DeleteCartItemAsync(cartItem.Item);
            }
        }

        public virtual async Task<IList<string>> CopyAsync(
            OrganizedShoppingCartItem cartItem,
            Customer customer,
            ShoppingCartType cartType,
            int storeId,
            bool automaticallyAddRequiredProductsIfEnabled)
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
                cartItem.Item.AttributeSelection,
                cartItem.Item.CustomerEnteredPrice,
                cartItem.Item.Quantity,
                automaticallyAddRequiredProductsIfEnabled,
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
                        childItem.Item.AttributeSelection,
                        childItem.Item.CustomerEnteredPrice,
                        childItem.Item.Quantity,
                        false,
                        addToCartContext);
                }
            }

            await AddToCartStoringAsync(addToCartContext);

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

        public async Task<decimal> GetOpenCartsSubTotalAsync()
        {
            var subTotal = await _db.ShoppingCartItems
                .Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart && x.Product != null)
                .SumAsync(x => (decimal?)(x.Product.Price * x.Quantity)) ?? decimal.Zero;

            return subTotal;
        }

        public async Task<decimal> GetOpenWishlistsSubTotalAsync()
        {
            var subTotal = await _db.ShoppingCartItems
                .Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.Wishlist && x.Product != null)
                .SumAsync(x => (decimal?)(x.Product.Price * x.Quantity)) ?? decimal.Zero;

            return subTotal;
        }
    }
}