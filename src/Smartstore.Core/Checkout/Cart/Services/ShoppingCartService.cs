using System.Linq.Dynamic.Core;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart service methods.
    /// </summary>
    public partial class ShoppingCartService : IShoppingCartService
    {
        // 0 = CustomerId, 1 = CartType, 2 = StoreId, 3 = Active.
        const string CartItemsKey = "shoppingcartitems:{0}-{1}-{2}-{3}";
        const string CartItemsPatternKey = "shoppingcartitems:*";

        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IRequestCache _requestCache;
        private readonly IEventPublisher _eventPublisher;
        private readonly IShoppingCartValidator _cartValidator;
        private readonly IRoundingHelper _roundingHelper;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly Lazy<ICheckoutFactory> _checkoutFactory;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly Currency _primaryCurrency;

        public ShoppingCartService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            IRequestCache requestCache,
            IEventPublisher eventPublisher,
            IShoppingCartValidator cartValidator,
            IRoundingHelper roundingHelper,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            Lazy<ICheckoutFactory> checkoutFactory,
            ICurrencyService currencyService,
            RewardPointsSettings rewardPointsSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _requestCache = requestCache;
            _eventPublisher = eventPublisher;
            _cartValidator = cartValidator;
            _roundingHelper = roundingHelper;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _checkoutFactory = checkoutFactory;
            _rewardPointsSettings = rewardPointsSettings;
            _shoppingCartSettings = shoppingCartSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task AddItemToCartAsync(AddToCartContext ctx)
        {
            Guard.NotNull(ctx);
            Guard.NotNull(ctx.Item);

            var customer = ctx.Customer ?? _workContext.CurrentCustomer;
            if (customer.IsBot())
            {
                return;
            }

            customer.ShoppingCartItems.Add(ctx.Item);
            await _db.SaveChangesAsync();

            if (ctx.ChildItems.Count > 0)
            {
                foreach (var childItem in ctx.ChildItems)
                {
                    childItem.ParentItemId = ctx.Item.Id;
                }

                customer.ShoppingCartItems.AddRange(ctx.ChildItems);
                await _db.SaveChangesAsync();
            }
        }

        public virtual async Task<bool> AddToCartAsync(AddToCartContext ctx)
        {
            Guard.NotNull(ctx);

            // This is called when customer adds a product to cart
            ctx.Customer ??= _workContext.CurrentCustomer;
            ctx.StoreId ??= _storeContext.CurrentStore.Id;
            
            if (ctx.Customer.IsBot())
            {
                ctx.Warnings.Add(T("Common.Error.BotsNotPermitted"));
                return false;
            }

            ctx.Customer.ResetCheckoutData(ctx.StoreId.Value);
            await _db.SaveChangesAsync();

            // Get raw attributes from variant query.
            if (ctx.VariantQuery != null)
            {
                await _db.LoadCollectionAsync(ctx.Product, x => x.ProductVariantAttributes, false);

                var (selection, warnings) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(
                    ctx.VariantQuery,
                    ctx.Product.ProductVariantAttributes,
                    ctx.Product.Id,
                    ctx.BundleItemId,
                    false);

                if (ctx.Product.IsGiftCard)
                {
                    var giftCardInfo = ctx.VariantQuery.GetGiftCardInfo(ctx.Product.Id, ctx.BundleItemId);
                    selection.AddGiftCardInfo(giftCardInfo);
                }

                ctx.RawAttributes = selection.AsJson();
            }

            if (ctx.Product.ProductType == ProductType.BundledProduct && ctx.AttributeSelection.AttributesMap.Any())
            {
                ctx.Warnings.Add(T("ShoppingCart.Bundle.NoAttributes"));

                // What's this for? It looks like a hack:
                if (ctx.BundleItem != null)
                    return false;
            }

            if (!await _cartValidator.ValidateAccessPermissionsAsync(ctx.Customer, ctx.CartType, ctx.Warnings))
            {
                return false;
            }

            var cart = await GetCartAsync(ctx.Customer, ctx.CartType, ctx.StoreId.Value);

            if (ctx.AutomaticallyAddRequiredProducts)
            {
                await AddRequiredProductsAsync(cart, ctx);
            }

            var existingCartItem = ctx.BundleItem == null
                ? FindItemInCart(cart, ctx.CartType, ctx.Product, ctx.AttributeSelection, ctx.CustomerEnteredPrice)?.Item
                : null;

            // Add item to cart (if no warnings accured)
            if (existingCartItem != null && !_shoppingCartSettings.AddProductsToBasketInSinglePositions)
            {
                // Product is already in cart, find existing item
                var newQuantity = ctx.Quantity + existingCartItem.Quantity;

                existingCartItem.Quantity = newQuantity;
                existingCartItem.UpdatedOnUtc = DateTime.UtcNow;
                existingCartItem.RawAttributes = ctx.AttributeSelection.AsJson();

                if (!await _cartValidator.ValidateAddToCartItemAsync(ctx, existingCartItem, cart.Items))
                {
                    return false;
                }

                // Update cart item
                await _db.SaveChangesAsync();
                return true;
            }
            else
            {
                if (!_cartValidator.ValidateItemsMaximumCartQuantity(ctx.CartType, cart.Items.Length, ctx.Warnings))
                {
                    return false;
                }

                // Product is not in cart yet, create new item
                var cartItem = new ShoppingCartItem
                {
                    CustomerEnteredPrice = ctx.CustomerEnteredPrice.Amount,
                    RawAttributes = ctx.RawAttributes,
                    ShoppingCartType = ctx.CartType,
                    StoreId = ctx.StoreId.Value,
                    Quantity = ctx.Quantity,
                    Customer = ctx.Customer,
                    Product = ctx.Product,
                    ProductId = ctx.Product.Id,
                    ParentItemId = null,
                    BundleItemId = ctx.BundleItem?.Id,
                    BundleItem = ctx.BundleItem
                };

                // Validate shopping cart item
                if (!await _cartValidator.ValidateAddToCartItemAsync(ctx, cartItem, cart.Items))
                {
                    return false;
                }

                // Checks whether the product is the parent item of a bundle, or just a simple product.
                if (ctx.BundleItem == null)
                {
                    // Set cart item as item for simple & bundle products, only if its not set by the caller
                    ctx.Item ??= cartItem;
                }
                else
                {
                    // Add item as child of bundle
                    ctx.ChildItems.Add(cartItem);
                }
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            // If ctx.Product is a bundle product and the setting to automatically add bundle products is true, try to add all corresponding BundleItems.

            if (ctx.AutomaticallyAddBundleProducts
                && ctx.Product.ProductType == ProductType.BundledProduct
                && ctx.BundleItem == null
                && ctx.Warnings.Count == 0)
            {
                var bundleItems = await _db.ProductBundleItem
                    .ApplyBundledProductsFilter([ctx.Product.Id], true)
                    .Include(x => x.Product)
                    .ToListAsync();

                foreach (var bundleItem in bundleItems)
                {
                    bundleItem.BundleProduct = ctx.Item.Product;

                    var bundleItemContext = new AddToCartContext
                    {
                        StoreId = ctx.StoreId,
                        Customer = ctx.Customer,
                        CartType = ctx.CartType,
                        BundleItem = bundleItem,
                        ChildItems = ctx.ChildItems,
                        Product = bundleItem.Product,
                        Quantity = bundleItem.Quantity,
                        VariantQuery = ctx.VariantQuery,
                        RawAttributes = ctx.RawAttributes,
                        CustomerEnteredPrice = ctx.CustomerEnteredPrice,
                        AutomaticallyAddRequiredProducts = ctx.AutomaticallyAddRequiredProducts,
                    };

                    if (!await AddToCartAsync(bundleItemContext))
                    {
                        ctx.Warnings.AddRange(bundleItemContext.Warnings);
                        break;
                    }
                }
            }

            // Add item and its children (if active) to the cart, when it is either a simple product or
            // if it is the parent item of its bundle (bundleItem = null) and no warnings occurred.            
            if (ctx.BundleItem == null && ctx.Warnings.Count == 0)
            {
                await AddItemToCartAsync(ctx);
            }

            return ctx.Warnings.Count == 0;
        }

        public virtual async Task<bool> CopyAsync(AddToCartContext ctx)
        {
            Guard.NotNull(ctx);

            var childItems = ctx.ChildItems;
            ctx.ChildItems = [];

            foreach (var childItem in childItems)
            {
                var childCtx = new AddToCartContext
                {
                    Customer = ctx.Customer,
                    CartType = ctx.CartType,
                    StoreId = ctx.StoreId,
                    BundleItem = childItem.BundleItem,
                    Product = childItem.Product,
                    Quantity = childItem.Quantity,
                    RawAttributes = childItem.RawAttributes,
                    CustomerEnteredPrice = new(childItem.CustomerEnteredPrice, ctx.CustomerEnteredPrice.Currency),
                    ChildItems = ctx.ChildItems
                };

                if (!await AddToCartAsync(childCtx))
                {
                    ctx.Warnings.AddRange(childCtx.Warnings);
                }
            }

            if (ctx.Warnings.Count > 0 || !await AddToCartAsync(ctx))
            {
                return false;
            }

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return ctx.Warnings.Count == 0;
        }

        public virtual async Task DeleteCartItemAsync(ShoppingCartItem cartItem, bool resetCheckoutData = true, bool removeInvalidCheckoutAttributes = false)
        {
            Guard.NotNull(cartItem);

            var customer = cartItem.Customer;
            var storeId = cartItem.StoreId;

            if (resetCheckoutData)
            {
                customer?.ResetCheckoutData(storeId);
            }

            _db.ShoppingCartItems.Remove(cartItem);

            // Delete child cart items.
            if (customer != null)
            {
                var childItems = await _db.ShoppingCartItems
                    .Where(x => x.CustomerId == customer.Id && x.ParentItemId != null && x.ParentItemId.Value == cartItem.Id && x.Id != cartItem.Id)
                    .ToListAsync();

                _db.ShoppingCartItems.RemoveRange(childItems);
            }

            await _db.SaveChangesAsync();

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            if (removeInvalidCheckoutAttributes && cartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && customer != null)
            {
                await RemoveInvalidCheckoutAttributesAsync(customer, storeId);
            }
        }

        public virtual async Task<int> DeleteCartAsync(ShoppingCart cart, bool resetCheckoutData = true, bool removeInvalidCheckoutAttributes = false)
        {
            Guard.NotNull(cart);

            var itemsToDelete = new List<ShoppingCartItem>(cart.Items.Select(x => x.Item));

            // Add child items (like bundle items).
            foreach (var item in cart.Items)
            {
                itemsToDelete.AddRange(item.ChildItems.Select(x => x.Item));
            }

            _db.ShoppingCartItems.RemoveRange(itemsToDelete);

            await _db.SaveChangesAsync();

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            if (resetCheckoutData)
            {
                cart.Customer.ResetCheckoutData(cart.StoreId);
            }

            if (removeInvalidCheckoutAttributes && cart.CartType == ShoppingCartType.ShoppingCart)
            {
                await RemoveInvalidCheckoutAttributesAsync(cart.Customer, cart.StoreId);
            }

            return itemsToDelete.Count;
        }

        public virtual Task<ShoppingCart> GetCartAsync(
            Customer customer = null,
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart, 
            int storeId = 0,
            bool? activeOnly = true)
        {
            customer ??= _workContext.CurrentCustomer;

            if (!_shoppingCartSettings.AllowActivatableCartItems)
            {
                // Always load all items, regardless of whether they are active or inactive.
                activeOnly = null;
            }

            var cacheKey = CartItemsKey.FormatInvariant(customer.Id, (int)cartType, storeId, activeOnly);

            var result = _requestCache.GetAsync(cacheKey, async () =>
            {
                await LoadCartItemCollection(customer);
                var cartItems = customer.ShoppingCartItems.FilterByCartType(cartType, storeId, activeOnly).ToList();

                // Perf: Prefetch (load) all attribute values in any of the attribute definitions across all cart items (including any bundle part).
                await _productAttributeMaterializer.PrefetchProductVariantAttributesAsync(cartItems.Select(x => x.AttributeSelection));

                var organizedItems = await OrganizeCartItemsAsync(cartItems);

                return new ShoppingCart(customer, storeId, organizedItems)
                {
                    CartType = cartType,
                    Requirements = _checkoutFactory.Value.GetRequirements()
                };
            });

            return result;
        }

        public virtual async Task<int> CountProductsInCartAsync(
            Customer customer = null,
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart,
            int storeId = 0,
            bool? activeOnly = true)
        {
            customer ??= _workContext.CurrentCustomer;

            if (!_shoppingCartSettings.AllowActivatableCartItems)
            {
                activeOnly = null;
            }

            var cacheKey = CartItemsKey.FormatInvariant(customer.Id, (int)cartType, storeId, activeOnly);
            var cart = _requestCache.Get<ShoppingCart>(cacheKey, null);
            if (cart != null)
            {
                return cart.GetTotalQuantity();
            }

            await LoadCartItemCollection(customer);

            return customer.ShoppingCartItems
                .FilterByCartType(cartType, storeId, activeOnly, false)
                .Sum(x => (int?)x.Quantity) ?? 0;
        }

        public virtual async Task<bool> MigrateCartAsync(Customer fromCustomer, Customer toCustomer)
        {
            Guard.NotNull(fromCustomer);
            Guard.NotNull(toCustomer);

            if (fromCustomer.Id == toCustomer.Id || toCustomer.IsBot())
            {
                return false;
            }

            var cartItems = await OrganizeCartItemsAsync(fromCustomer.ShoppingCartItems);
            if (cartItems.Count == 0)
            {
                return false;
            }

            var result = true;
            var firstItem = cartItems[0].Item;

            foreach (var cartItem in cartItems)
            {
                var ctx = new AddToCartContext
                {
                    Product = cartItem.Item.Product,
                    RawAttributes = cartItem.Item.AttributeSelection.AsJson(),
                    CustomerEnteredPrice = new(cartItem.Item.CustomerEnteredPrice, _primaryCurrency),
                    Quantity = cartItem.Item.Quantity,
                    ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList(),
                    Customer = toCustomer,
                    CartType = cartItem.Item.ShoppingCartType,
                    StoreId = cartItem.Item.StoreId,
                };

                if (!await CopyAsync(ctx))
                {
                    result = false;
                }
            }

            if (fromCustomer != null && toCustomer != null)
            {
                await _eventPublisher.PublishAsync(new MigrateShoppingCartEvent(fromCustomer, toCustomer, firstItem.StoreId));
            }

            var cart = new ShoppingCart(fromCustomer, firstItem.StoreId, cartItems)
            {
                CartType = firstItem.ShoppingCartType,
                Requirements = _checkoutFactory.Value.GetRequirements()
            };

            await DeleteCartAsync(cart);

            return result;
        }

        public virtual async Task<IList<string>> UpdateCartItemAsync(
            Customer customer, 
            int cartItemId,
            int? quantity, 
            bool? active,
            bool resetCheckoutData = false)
        {
            Guard.NotNull(customer);

            await LoadCartItemCollection(customer);

            var warnings = new List<string>();
            var cartItem = customer.ShoppingCartItems.FirstOrDefault(x => x.Id == cartItemId && x.ParentItemId == null);
            if (cartItem == null)
            {
                return warnings;
            }

            if (quantity <= 0)
            {
                await DeleteCartItemAsync(cartItem, resetCheckoutData, true);
                return warnings;
            }

            if (resetCheckoutData)
            {
                customer.ResetCheckoutData(cartItem.StoreId);
            }

            var ctx = new AddToCartContext
            {
                Customer = customer,
                CartType = cartItem.ShoppingCartType,
                Product = cartItem.Product,
                StoreId = cartItem.StoreId,
                RawAttributes = cartItem.AttributeSelection.AsJson(),
                CustomerEnteredPrice = new Money(cartItem.CustomerEnteredPrice, _primaryCurrency),
                Quantity = quantity ?? cartItem.Quantity,
                AutomaticallyAddRequiredProducts = false,
            };

            cartItem.Active = active ?? cartItem.Active;
            cartItem.Quantity = quantity ?? cartItem.Quantity;
            cartItem.UpdatedOnUtc = DateTime.UtcNow;

            var cart = await GetCartAsync(customer, cartItem.ShoppingCartType, cartItem.StoreId);

            // INFO: we execute SaveChangesAsync despite warnings because the quantity on cart page
            // must be updatable at all times (see issue #621).
            if (!await _cartValidator.ValidateAddToCartItemAsync(ctx, cartItem, cart.Items))
            {
                warnings.AddRange(ctx.Warnings);
            }

            await _db.SaveChangesAsync();

            _requestCache.RemoveByPattern(CartItemsPatternKey);

            return warnings;
        }

        public virtual async Task<bool> SaveCartDataAsync(
            ShoppingCart cart,
            IList<string> warnings,
            ProductVariantQuery query,
            bool? useRewardPoints = null,
            bool resetCheckoutData = true,
            bool validateCheckoutAttributes = true)
        {
            cart ??= await GetCartAsync(storeId: _storeContext.CurrentStore.Id);

            if (cart.Customer.IsBot())
            {
                warnings.Add(T("Common.Error.BotsNotPermitted"));
                return false;
            }

            if (resetCheckoutData)
            {
                // Clear payment and shipping method selected in checkout.
                cart.Customer.ResetCheckoutData(cart.StoreId);
            }

            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);

            if (_rewardPointsSettings.Enabled && useRewardPoints.HasValue)
            {
                cart.Customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints.Value;
            }

            // INFO: we must save before validating the cart.
            await _db.SaveChangesAsync();

            return await _cartValidator.ValidateCartAsync(cart, warnings, validateCheckoutAttributes);
        }

        public virtual OrganizedShoppingCartItem FindItemInCart(
            ShoppingCart cart,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection = null,
            Money? customerEnteredPrice = null)
        {
            Guard.NotNull(cart);
            Guard.NotNull(product);

            // Return on product bundle with individual item pricing. It is too complex to compare.
            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
            {
                return null;
            }

            // Filter items of matching cart type, product ID and product type.
            var filteredCart = cart.Items.Where(x => x.Item.ShoppingCartType == shoppingCartType &&
                x.Item.ParentItemId == null &&
                x.Item.Product.ProductTypeId == product.ProductTypeId &&
                x.Item.ProductId == product.Id);

            foreach (var cartItem in filteredCart)
            {
                var item = cartItem.Item;
                var giftCardInfoSame = true;
                var customerEnteredPricesEqual = true;
                var attributesEqual = item.AttributeSelection == selection;

                if (item.Product.IsGiftCard)
                {
                    var info1 = item.AttributeSelection?.GetGiftCardInfo();
                    var info2 = selection?.GetGiftCardInfo();

                    if (info1 != null && info2 != null)
                    {
                        // INFO: in this context, we only compare the name of recipient and sender.
                        if (!info1.RecipientName.EqualsNoCase(info2.RecipientName) || !info1.SenderName.EqualsNoCase(info2.SenderName))
                        {
                            giftCardInfoSame = false;
                        }
                    }
                }

                // Products with CustomerEntersPrice are equal if the price is the same.
                // But a system product may only be placed once in the shopping cart.
                if (customerEnteredPrice.HasValue && item.Product.CustomerEntersPrice && !item.Product.IsSystemProduct)
                {
                    var currency = customerEnteredPrice?.Currency ?? _workContext.WorkingCurrency;
                    customerEnteredPricesEqual = _roundingHelper.Round(item.CustomerEnteredPrice, currency) == _roundingHelper.Round(customerEnteredPrice.Value.Amount, currency);
                }

                if (attributesEqual && giftCardInfoSame && customerEnteredPricesEqual)
                {
                    return cartItem;
                }
            }

            return null;
        }

        protected virtual async Task<List<OrganizedShoppingCartItem>> OrganizeCartItemsAsync(ICollection<ShoppingCartItem> items)
        {
            var result = new List<OrganizedShoppingCartItem>();

            if (items.IsNullOrEmpty())
            {
                return result;
            }

            // Bundle items that require merging of attribute combinations.
            var mergeRequiringItems = new List<ShoppingCartItem>();
            var childItemsMap = items.ToMultimap(x => x.ParentItemId ?? 0, x => x);

            foreach (var parent in items.Where(x => x.ParentItemId == null).OrderBy(x => x.Id))
            {
                var parentItem = CreateOrganizedCartItem(parent);

                if (childItemsMap.TryGetValues(parent.Id, out var children))
                {
                    parentItem.ChildItems.AddRange(children.Select(CreateOrganizedCartItem));

                    if (parent.Product?.BundlePerItemPricing ?? false)
                    {
                        // Get cart items where we have to consider attribute combination prices of bundle items.
                        mergeRequiringItems.AddRange(children.Where(x => x.RawAttributes.HasValue() && x.BundleItem != null));
                    }
                }

                result.Add(parentItem);
            }

            if (mergeRequiringItems.Count > 0)
            {
                await _productAttributeMaterializer.MergeWithCombinationAsync(mergeRequiringItems);
            }

            return result;
        }

        /// <summary>
        /// Removes invalid checkout attributes. For example if the attribute requires shipping but the cart does not require shipping at all.
        /// </summary>
        protected virtual async Task<int> RemoveInvalidCheckoutAttributesAsync(Customer customer, int storeId)
        {
            var attributeSelection = customer.GenericAttributes.CheckoutAttributes;
            if (!attributeSelection.HasAttributes)
            {
                return 0;
            }

            var idsToRemove = new HashSet<int>();
            var attributes = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(attributeSelection);

            var cart = await GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            if (!cart.IsShippingRequired)
            {
                idsToRemove.AddRange(attributes
                    .Where(x => x.ShippableProductRequired)
                    .Select(x => x.Id));
            }

            idsToRemove.AddRange(attributes
                .Where(x => !x.IsActive)
                .Select(x => x.Id));

            if (idsToRemove.Count > 0)
            {
                attributeSelection.RemoveAttributes(idsToRemove);
                customer.GenericAttributes.CheckoutAttributes = attributeSelection;

                await _db.SaveChangesAsync();
            }

            return idsToRemove.Count;
        }

        protected virtual async Task AddRequiredProductsAsync(ShoppingCart cart, AddToCartContext ctx)
        {
            var productIds = ctx.Product.ParseRequiredProductIds();
            if (productIds.IsNullOrEmpty())
            {
                return;
            }

            var missingProductIds = productIds.Except(cart.Items.Select(x => x.Item.ProductId));
            var missingProducts = await _db.Products.GetManyAsync(missingProductIds, false);
            var items = new List<OrganizedShoppingCartItem>(cart.Items);
            var newItems = new List<OrganizedShoppingCartItem>();

            var attributesMap = (await _db.ProductVariantAttributes
                .AsNoTracking()
                .Include(x => x.ProductVariantAttributeValues)
                .Where(x => missingProductIds.Contains(x.ProductId) && x.IsRequired)
                .ToListAsync())
                .ToMultimap(x => x.ProductId, x => x);

            foreach (var product in missingProducts)
            {
                // Get preselected values of required attributes.
                var attributeSelection = new ProductVariantAttributeSelection(null);

                if (attributesMap.TryGetValues(product.Id, out var attributes))
                {
                    foreach (var attribute in attributes.Where(x => x.IsListTypeAttribute()).OrderBy(x => x.Id))
                    {
                        var attributeValues = attribute.ProductVariantAttributeValues.Where(x => x.IsPreSelected).ToArray();
                        if (attributeValues.Length > 0)
                        {
                            attributeSelection.AddAttribute(attribute.Id, attributeValues.Select(x => (object)x.Id));
                        }
                    }
                }

                var item = new ShoppingCartItem
                {
                    CustomerEnteredPrice = ctx.CustomerEnteredPrice.Amount,
                    RawAttributes = attributeSelection.AsJson(),
                    ShoppingCartType = ctx.CartType,
                    StoreId = ctx.StoreId.Value,
                    Quantity = 1,
                    Customer = ctx.Customer,
                    Product = product,
                    BundleItemId = ctx.BundleItem?.Id
                };

                newItems.Add(CreateOrganizedCartItem(item));
            }

            items.AddRange(newItems);

            // Check whether required products are still missing.
            if (await _cartValidator.ValidateRequiredProductsAsync(ctx.Product, items, ctx.Warnings))
            {
                foreach (var item in newItems)
                {
                    await AddItemToCartAsync(new()
                    {
                        Item = item.Item,
                        ChildItems = ctx.ChildItems,
                        Customer = ctx.Customer
                    });
                }
            }
        }

        private async Task LoadCartItemCollection(Customer customer, bool force = false)
        {
            await _db.LoadCollectionAsync(customer, x => x.ShoppingCartItems, force, x =>
            {
                return x.Include(y => y.Product)
                    .ThenInclude(y => y.ProductVariantAttributes);
            });
        }

        private OrganizedShoppingCartItem CreateOrganizedCartItem(ShoppingCartItem item)
            => new(item, !_shoppingCartSettings.AllowActivatableCartItems || item.Active);
    }
}