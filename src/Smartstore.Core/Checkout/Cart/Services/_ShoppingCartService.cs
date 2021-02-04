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
using Smartstore.Core.Identity;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data.Batching;

// TODO: (ms) (core) (wip) needs orderTotalCalculationService and TESTING!
namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart service methods
    /// </summary>
    public partial class ShoppingCartService : IShoppingCartService
    {
        // 0 = CustomerId, 1 = CartType, 2 = StoreId
        const string CartItemsKey = "sm.cartitems-{0}-{1}-{2}";
        const string CartItemsPatternKey = "sm.cartitems-*";

        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IRequestCache _requestCache;
        private readonly IShoppingCartValidator _cartValidator;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        //private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        public ShoppingCartService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            IRequestCache requestCache,
            IShoppingCartValidator cartValidator,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer
            //,IOrderTotalCalculationService orderTotalCalculationService,
            )
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _requestCache = requestCache;
            _cartValidator = cartValidator;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            //_orderTotalCalculationService = orderTotalCalculationService;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        protected virtual async Task AddItemToCartAsync(AddToCartContext ctx)
        {
            if (ctx.Item != null)
            {
                ctx.Customer.ShoppingCartItems.Add(ctx.Item);

                if (!ctx.ChildItems.IsNullOrEmpty())
                {
                    foreach (var childItem in ctx.ChildItems)
                    {
                        childItem.ParentItemId = ctx.Item.Id;
                    }

                    ctx.Customer.ShoppingCartItems.AddRange(ctx.ChildItems);
                }

                _db.TryChangeState(ctx.Customer, EntityState.Modified);
                await _db.SaveChangesAsync();
            }
        }

        protected virtual async Task<IList<OrganizedShoppingCartItem>> OrganizeCartItemsAsync(ICollection<ShoppingCartItem> cart)
        {
            var result = new List<OrganizedShoppingCartItem>();

            if (cart.IsNullOrEmpty())
                return result;

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
                            childItem.BundleItemData.AdditionalCharge += attributeValues.Sum(x => x.PriceAdjustment);
                        }
                    }

                    parentItem.ChildItems.Add(childItem);
                }

                result.Add(parentItem);
            }

            return result;
        }

        // TODO: (ms) (core) AddToCartContext needs to have bundleItem and childItems already included correctly, they get just added...
        public virtual async Task<IList<string>> AddToCartAsync(AddToCartContext ctx)
        {
            Guard.NotNull(ctx, nameof(ctx));

            // This is called when customer adds a product to cart
            var warnings = new List<string>();
            ctx.Customer ??= _workContext.CurrentCustomer;
            ctx.StoreId ??= _storeContext.CurrentStore.Id;


            // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
            //_customerService.ResetCheckoutData(customer, storeId);

            // Checks whether attributes have been selected
            // TODO: (ms) (core) VariantQuery is missing. Tries to get attributes of product.
            //if (ctx.VariantQuery != null)
            //{
            //    // Create attribute selection from  product attributes
            //    var attributes = _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(ctx.AttributeSelection);
            //    //ctx.RawAttributes = ctx.VariantQuery.CreateSelectedAttributesXml(ctx.Product.Id, ctx.BundleItemId, attributes, _productAttributeParser,
            //    //_localizationService, _downloadService, _catalogSettings, null, ctx.Warnings);

            //    // Check context for bundle item errors
            //    if (ctx.Product.ProductType == ProductType.BundledProduct && ctx.RawAttributes.HasValue())
            //    {
            //        ctx.Warnings.Add(T("ShoppingCart.Bundle.NoAttributes"));

            //        if (ctx.BundleItem != null)
            //            return ctx.Warnings;
            //    }
            //}

            warnings.AddRange(await _cartValidator.ValidateAccessPermissionsAsync(ctx));
            if (warnings.Count > 0)
            {
                return warnings;
            }


            // TODO: (ms) (core) Call ValidateRequiredProductsAsync and check product for required products, that are not included in cart
            // add them if neccessary and wanted
            //ctx.AutomaticallyAddRequiredProductsIfEnabled

            var shoppingCart = await GetCartItemsAsync(ctx.Customer, ctx.CartType, ctx.StoreId.Value);
            warnings.AddRange(await _cartValidator.ValidateRequiredProductsAsync(ctx, shoppingCart));
            //if(ctx.AutomaticallyAddRequiredProductsIfEnabled)
            // Add missing products.....

            OrganizedShoppingCartItem existingCartItem = null;

            if (ctx.BundleItem == null)
            {
                existingCartItem = shoppingCart.FindItemInCart(ctx.CartType, ctx.Product, ctx.AttributeSelection, ctx.CustomerEnteredPrice);
            }

            // Add item to cart (if no warnings accured)
            if (existingCartItem != null)
            {
                // Product is already in cart, find existing item
                ctx.Quantity += existingCartItem.Item.Quantity;

                warnings.AddRange(await _cartValidator.ValidateCartItemAsync(ctx, shoppingCart));
                if (warnings.Count > 0)
                {
                    return warnings;
                }

                // Update cart item
                existingCartItem.Item.Quantity = ctx.Quantity;
                existingCartItem.Item.UpdatedOnUtc = DateTime.UtcNow;
                existingCartItem.Item.RawAttributes = ctx.RawAttributes;
                _db.TryChangeState(ctx.Customer, EntityState.Modified);
                await _db.SaveChangesAsync();
            }
            else
            {
                warnings.AddRange(await _cartValidator.ValidateCartItemAsync(ctx, shoppingCart));
                if (warnings.Count > 0)
                {
                    return warnings;
                }

                var warning = _cartValidator.ValidateCartItemsMaximum(ctx.CartType, shoppingCart.Count);
                if (warning.Count > 0)
                {
                    ctx.Warnings.AddRange(warning);
                    warnings.AddRange(warning);
                    return warnings;
                }

                // Product is not in cart yet, create new item
                var cartItem = new ShoppingCartItem
                {
                    CustomerEnteredPrice = ctx.CustomerEnteredPrice,
                    RawAttributes = ctx.RawAttributes,
                    ShoppingCartType = ctx.CartType,
                    StoreId = ctx.StoreId.Value,
                    Quantity = ctx.Quantity,
                    Customer = ctx.Customer,
                    Product = ctx.Product,
                    ParentItemId = null,
                    BundleItemId = ctx.BundleItem?.Id
                };

                // If product is no bundle, add it as cartItem
                if (ctx.BundleItem == null)
                {
                    Debug.Assert(ctx.Item == null, "Add to cart item already specified");
                    ctx.Item = cartItem;
                }
                else
                {
                    ctx.ChildItems.Add(cartItem);
                }
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            // If ctx.Product is a bundle product, try adding all corresponding bundleItems
            if (ctx.Product.ProductType == ProductType.BundledProduct && ctx.BundleItem == null && warnings.Count == 0)
            {
                var bundleItems = new List<ProductBundleItem>();

                // Get all bundle items and add each to the cart
                if (_db.IsCollectionLoaded(ctx.Product, x => x.ProductBundleItems))
                {
                    bundleItems = ctx.Product.ProductBundleItems.ToList();
                }
                else
                {
                    bundleItems = await _db.ProductBundleItem
                        .ApplyBundledProductsFilter(new[] { ctx.Product.Id })
                        .ToListAsync();
                }

                foreach (var bundleItem in bundleItems)
                {
                    // Try add each bundleItem to the cart
                    warnings.AddRange(
                        await AddToCartAsync(
                            new AddToCartContext
                            {
                                Item = ctx.Item,
                                Customer = ctx.Customer,
                                BundleItem = bundleItem,
                                Warnings = ctx.Warnings,
                                CartType = ctx.CartType,
                                StoreId = ctx.StoreId.Value,
                                ChildItems = ctx.ChildItems,
                                Product = bundleItem.Product,
                                Quantity = bundleItem.Quantity,
                                //VariantQuery = ctx.VariantQuery,
                                AutomaticallyAddRequiredProductsIfEnabled = ctx.AutomaticallyAddRequiredProductsIfEnabled
                            })
                        );

                    // If bundleItem could not be added to the shopping cart, remove child items as they 
                    if (warnings.Count > 0)
                    {
                        ctx.ChildItems.Clear();
                        break;
                    }
                }
            }

            // If context is no bundleItem, add item (parent) and its children (grouped product)
            if (ctx.BundleItem == null && warnings.Count == 0)
            {
                await AddItemToCartAsync(ctx);
            }

            return warnings;
        }

        public virtual async Task<IList<string>> CopyAsync(AddToCartContext ctx)
        {
            Guard.NotNull(ctx, nameof(ctx));

            var warnings = await AddToCartAsync(ctx);
            if (warnings.Count > 0 || ctx.ChildItems == null)
                return warnings;

            foreach (var childItem in ctx.ChildItems)
            {
                ctx.BundleItem = childItem.BundleItem;
                ctx.Product = childItem.Product;
                ctx.Quantity = childItem.Quantity;
                ctx.RawAttributes = childItem.AttributeSelection.AsJson();
                ctx.CustomerEnteredPrice = childItem.CustomerEnteredPrice;
                ctx.AutomaticallyAddRequiredProductsIfEnabled = false;

                warnings.AddRange(await AddToCartAsync(ctx));
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return warnings;
        }

        // TODO: (ms) (core) Find other methods where navigation properties are (maybe) accessed > check for IsLoaded and use instead of db call
        // TODO: (ms) (core) Make sure Async methods (check calls of methods in services) are always awaited
        public virtual Task<int> CountCartItemsAsync(
            Customer customer = null,
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart,
            int storeId = 0)
        {
            customer ??= _workContext.CurrentCustomer;

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
        
        public virtual async Task<int> DeleteCartItemsAsync(
            IEnumerable<ShoppingCartItem> cartItems,
            bool resetCheckoutData = true,
            bool removeInvalidCheckoutAttributes = false,
            bool deleteChildCartItems = true)
        {
            Guard.NotNull(cartItems, nameof(cartItems));

            var customer = cartItems.Select(x => x.Customer).FirstOrDefault();
            if (resetCheckoutData && customer != null)
            {
                // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
                //_customerService.ResetCheckoutData(shoppingCartItem.Customer, shoppingCartItem.StoreId);
            }

            _db.RemoveRange(cartItems);
            _requestCache.RemoveByPattern(CartItemsPatternKey);

            var storeId = cartItems.Select(x => x.StoreId).FirstOrDefault();
            var cartItemIds = cartItems.Select(x => x.Id).ToList();
            var cartType = cartItems.Select(x => x.ShoppingCartType).FirstOrDefault();

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
                var attributeSelection = customer.GenericAttributes.CheckoutAttributes;
                var attributes = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(attributeSelection);
                var organizedCartItems = await GetCartItemsAsync(customer, storeId: storeId);
                var attributeIdsToRemove = attributes.GetInvalidShippableAttributesIds(organizedCartItems);

                attributeSelection.RemoveAttributes(attributeIdsToRemove);
                customer.GenericAttributes.CheckoutAttributes = attributeSelection;
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
        
        public virtual Task<IList<OrganizedShoppingCartItem>> GetCartItemsAsync(
            Customer customer = null,
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart,
            int storeId = 0)
        {
            customer ??= _workContext.CurrentCustomer;

            var cacheKey = CartItemsKey.FormatInvariant(customer.Id, (int)cartType, storeId);
            var result = _requestCache.Get(cacheKey, async () =>
            {
                var cartItems = new List<ShoppingCartItem>();
                // TODO: (ms) (core) Do we need to check for ShoppingCartItems.Product.ProductVariantAttribute is loaded too? Would this direct access be even possible then?
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

        public virtual async Task<decimal> GetOpenCartsSubTotalAsync()
        {
            var subTotal = await _db.ShoppingCartItems
                .Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart && x.Product != null)
                .SumAsync(x => (decimal?)(x.Product.Price * x.Quantity)) ?? decimal.Zero;

            return subTotal;
        }

        public virtual async Task<decimal> GetOpenWishlistsSubTotalAsync()
        {
            var subTotal = await _db.ShoppingCartItems
                .Where(x => x.ShoppingCartTypeId == (int)ShoppingCartType.Wishlist && x.Product != null)
                .SumAsync(x => (decimal?)(x.Product.Price * x.Quantity)) ?? decimal.Zero;

            return subTotal;
        }

        public virtual async Task MigrateCartAsync(Customer fromCustomer, Customer toCustomer)
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

                await CopyAsync(
                    new AddToCartContext
                    {
                        Product = cartItem.Item.Product,
                        RawAttributes = cartItem.Item.AttributeSelection.AsJson(),
                        CustomerEnteredPrice = cartItem.Item.CustomerEnteredPrice,
                        Quantity = cartItem.Item.Quantity,
                        ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList(),
                        Customer = toCustomer,
                        CartType = cartItem.Item.ShoppingCartType,
                        StoreId = cartItem.Item.StoreId,
                        AutomaticallyAddRequiredProductsIfEnabled = false
                    }
                );
            }

            // TODO: (ms) (core) Implement publishMigrateShoppingCart
            //_eventPublisher.PublishMigrateShoppingCart(fromCustomer, toCustomer, storeId);

            await DeleteCartItemsAsync(cartItems.Select(x => x.Item));
        }

        public virtual async Task<IList<string>> UpdateCartItemAsync(Customer customer, int cartItemId, int newQuantity, bool resetCheckoutData)
        {
            Guard.NotNull(customer, nameof(customer));

            var warnings = new List<string>();

            var cartItem = customer.ShoppingCartItems.FirstOrDefault(x => x.Id == cartItemId && x.ParentItemId == null);
            if (cartItem == null)
                return warnings;

            if (resetCheckoutData)
            {
                // TODO: (ms) (core) customerService.ResetCheckoutData() is missing
                // _customerService.ResetCheckoutData(customer, shoppingCartItem.StoreId);
            }

            if (newQuantity > 0)
            {
                var ctx = new AddToCartContext
                {
                    Customer = customer,
                    CartType = cartItem.ShoppingCartType,
                    Product = cartItem.Product,
                    StoreId = cartItem.StoreId,
                    RawAttributes = cartItem.AttributeSelection.AsJson(),
                    CustomerEnteredPrice = cartItem.CustomerEnteredPrice,
                    Quantity = newQuantity,
                    AutomaticallyAddRequiredProductsIfEnabled = false
                };

                var cartItems = await GetCartItemsAsync(customer, cartItem.ShoppingCartType, cartItem.StoreId);

                warnings.AddRange(await _cartValidator.ValidateCartItemAsync(ctx, cartItems));
                if (warnings.Count == 0)
                {
                    cartItem.Quantity = newQuantity;
                    cartItem.UpdatedOnUtc = DateTime.UtcNow;
                    _db.TryChangeState(customer, EntityState.Modified);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                await DeleteCartItemsAsync(new[] { cartItem }, resetCheckoutData, true);
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return warnings;
        }
        
        // TODO: (ms) (core) Implement orderTotalCalculationService.GetShoppingCartSubTotal()
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

        // TODO: (ms) (core) Implement GetCurrentCartSubTotalAsync() > orderTotalCalculationService.GetShoppingCartSubTotal()
        //public virtual string GetFormattedCurrentCartSubTotal()
        //{
        //    return _priceFormatter.FormatPrice(GetCurrentCartSubTotalAsync());
        //}

        // TODO: (ms) (core) Implement GetCurrentCartSubTotalAsync() > orderTotalCalculationService.GetShoppingCartSubTotal()
        //public virtual string GetFormattedCurrentCartSubTotal(IList<OrganizedShoppingCartItem> cart)
        //{
        //    return _priceFormatter.FormatPrice(GetCurrentCartSubTotal(cart));
        //}
    }
}