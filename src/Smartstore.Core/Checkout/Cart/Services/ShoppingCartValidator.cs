using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart validation methods.
    /// </summary>
    public partial class ShoppingCartValidator : IShoppingCartValidator
    {
        private readonly SmartDbContext _db;
        private readonly IAclService _aclService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICurrencyService _currencyService;
        private readonly ShoppingCartSettings _cartSettings;
        private readonly IPermissionService _permissionService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ILocalizationService _localizationService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;

        public ShoppingCartValidator(
            SmartDbContext db,
            IAclService aclService,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICurrencyService currencyService,
            ShoppingCartSettings cartSettings,
            IPermissionService permissionService,
            IStoreMappingService storeMappingService,
            ILocalizationService localizationService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer)
        {
            _db = db;
            _aclService = aclService;
            _workContext = workContext;
            _storeContext = storeContext;
            _currencyService = currencyService;
            _cartSettings = cartSettings;
            _permissionService = permissionService;
            _storeMappingService = storeMappingService;
            _localizationService = localizationService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<bool> ValidateAccessPermissionsAsync(Customer customer, ShoppingCartType cartType, IList<string> warnings)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(warnings, nameof(warnings));

            var isValid = true;

            // Validate access permissions
            if (cartType == ShoppingCartType.ShoppingCart
                && !await _permissionService.AuthorizeAsync(Permissions.Cart.AccessShoppingCart, customer))
            {
                isValid = false;
                warnings.Add(T("ShoppingCart.IsDisabled"));
            }
            else if (cartType == ShoppingCartType.Wishlist
                && !await _permissionService.AuthorizeAsync(Permissions.Cart.AccessWishlist, customer))
            {
                isValid = false;
                warnings.Add(T("Wishlist.IsDisabled"));
            }

            return isValid;
        }

        public virtual bool ValidateBundleItem(ProductBundleItem bundleItem, IList<string> warnings)
        {
            Guard.NotNull(bundleItem, nameof(bundleItem));
            Guard.NotNull(warnings, nameof(warnings));

            var currentWarnings = new List<string>();

            var name = bundleItem.GetLocalizedName();

            if (!bundleItem.Published)
            {
                currentWarnings.Add(T("ShoppingCart.Bundle.BundleItemUnpublished", name));
            }

            if (bundleItem.ProductId == 0
                || bundleItem.Product == null
                || bundleItem.BundleProductId == 0
                || bundleItem.BundleProduct == null)
            {
                currentWarnings.Add(T("ShoppingCart.Bundle.MissingProduct", name));
            }

            if (bundleItem.Quantity <= 0)
            {
                currentWarnings.Add(T("ShoppingCart.Bundle.Quantity", name));
            }

            if (bundleItem.Product.IsDownload || bundleItem.Product.IsRecurring)
            {
                currentWarnings.Add(T("ShoppingCart.Bundle.ProductResrictions", name));
            }

            warnings.AddRange(currentWarnings);
            return !currentWarnings.Any();
        }

        public virtual async Task<bool> ValidateCartAsync(ShoppingCart cart, IList<string> warnings, bool validateCheckoutAttributes = false)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(warnings, nameof(warnings));

            var currentWarnings = new List<string>();

            var missingProduct = cart.Items.Where(x => x.Item.Product is null).FirstOrDefault();
            if (missingProduct != null)
            {
                currentWarnings.Add(T("ShoppingCart.CannotLoadProduct", missingProduct.Item.ProductId));
            }

            var hasNonRecurringProducts = cart.IncludesMatchingItems(x => !x.IsRecurring);
            var hasRecurringProducts = cart.IncludesMatchingItems(x => x.IsRecurring);

            // Do not mix recurring and non-recurring products
            if (hasNonRecurringProducts && hasRecurringProducts)
            {
                currentWarnings.Add(T("ShoppingCart.CannotMixStandardAndAutoshipProducts"));
            }

            // Validate recurring products
            if (hasRecurringProducts)
            {
                var recurringCycleInfo = cart.GetRecurringCycleInfo(_localizationService);
                if (recurringCycleInfo.ErrorMessage.HasValue())
                {
                    currentWarnings.Add(recurringCycleInfo.ErrorMessage);
                }
            }

            // Validate checkout attributes if defined.
            if (validateCheckoutAttributes)
            {
                var checkoutAttributes = cart?.Customer?.GenericAttributes?.CheckoutAttributes;
                if (checkoutAttributes != null)
                {
                    var existingAttributesQuery = _db.CheckoutAttributes.ApplyStoreFilter(_storeContext.CurrentStore.Id);
                    if (!cart.IsShippingRequired())
                    {
                        // No shipping required. Filter attributes which require shippable products.
                        existingAttributesQuery = existingAttributesQuery.Where(x => !x.ShippableProductRequired);
                    }

                    var selectedAttributes = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(checkoutAttributes);
                    var notSelectedAttributes = await existingAttributesQuery
                        .Where(x => x.IsRequired && !selectedAttributes.Contains(x))
                        .ToListAsync();

                    // Check for not selected attributes.
                    foreach (var attribute in notSelectedAttributes)
                    {
                        var textPrompt = attribute.TextPrompt.IsEmpty() ? attribute.GetLocalized(x => x.Name) : attribute.GetLocalized(x => x.TextPrompt);
                        currentWarnings.Add(T("ShoppingCart.SelectAttribute", textPrompt));
                    }
                }
            }

            warnings.AddRange(currentWarnings);

            return !currentWarnings.Any();
        }

        public virtual async Task<bool> ValidateAddToCartItemAsync(AddToCartContext ctx, ShoppingCartItem cartItem, IEnumerable<OrganizedShoppingCartItem> cartItems)
        {
            Guard.NotNull(ctx, nameof(ctx));
            Guard.NotNull(cartItem, nameof(cartItem));
            Guard.NotNull(cartItems, nameof(cartItems));

            var warnings = new List<string>();

            await ValidateProductAsync(cartItem, warnings, ctx.StoreId);
            await this.ValidateProductAttributesAsync(cartItem, cartItems, warnings);

            ValidateGiftCardInfo(cartItem.Product, cartItem.AttributeSelection, warnings);

            await ValidateRequiredProductsAsync(ctx.Product, cartItems, warnings);

            // Bundle and bundle items (child items) warnings
            if (ctx.BundleItem != null || !ctx.ChildItems.IsNullOrEmpty())
            {
                var bundleItem = ctx.BundleItem ?? ctx.ChildItems.Select(x => x.BundleItem).FirstOrDefault();
                if (bundleItem != null)
                {
                    ValidateBundleItem(bundleItem, warnings);
                }
            }

            ctx.Warnings.AddRange(warnings);
            return !warnings.Any();
        }

        public virtual bool ValidateItemsMaximumCartQuantity(ShoppingCartType cartType, int cartItemsCount, IList<string> warnings)
        {
            Guard.NotNull(warnings, nameof(warnings));

            var isValid = true;

            if (cartType == ShoppingCartType.ShoppingCart && cartItemsCount >= _cartSettings.MaximumShoppingCartItems)
            {
                isValid = false;
                warnings.Add(T("ShoppingCart.MaximumShoppingCartItems"));
            }
            else if (cartType == ShoppingCartType.Wishlist && cartItemsCount >= _cartSettings.MaximumWishlistItems)
            {
                isValid = false;
                warnings.Add(T("ShoppingCart.MaximumWishlistItems"));
            }

            return isValid;
        }

        public virtual bool ValidateGiftCardInfo(Product product, ProductVariantAttributeSelection selection, IList<string> warnings)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(selection, nameof(selection));
            Guard.NotNull(warnings, nameof(warnings));

            if (!product.IsGiftCard)
            {
                return true;
            }

            var currentWarnings = new List<string>();
            var giftCardInfo = selection.GetGiftCardInfo();
            var recipientName = giftCardInfo?.RecipientName;
            var recipientEmail = giftCardInfo?.RecipientEmail;
            var senderName = giftCardInfo?.SenderName;
            var senderEmail = giftCardInfo?.SenderEmail;

            if (recipientName.IsEmpty())
            {
                currentWarnings.Add(T("ShoppingCart.RecipientNameError"));
            }

            if (senderName.IsEmpty())
            {
                currentWarnings.Add(T("ShoppingCart.SenderNameError"));
            }

            if (product.GiftCardType == GiftCardType.Virtual)
            {
                if (!recipientEmail.IsEmail())
                {
                    currentWarnings.Add(T("ShoppingCart.RecipientEmailError"));
                }

                if (!senderEmail.IsEmail())
                {
                    currentWarnings.Add(T("ShoppingCart.SenderEmailError"));
                }
            }

            warnings.AddRange(currentWarnings);

            return !currentWarnings.Any();
        }

        public virtual async Task<bool> ValidateProductAsync(ShoppingCartItem cartItem, IList<string> warnings, int? storeId = null, int? quantity = null)
        {
            Guard.NotNull(cartItem, nameof(cartItem));
            Guard.NotNull(warnings, nameof(warnings));

            var product = cartItem.Product;
            if (product == null)
            {
                warnings.Add(T("Products.NotFound", cartItem.ProductId));
                return false;
            }

            if (product.Deleted)
            {
                warnings.Add(T("ShoppingCart.ProductDeleted"));
                return false;
            }

            var currentWarnings = new List<string>();

            // Grouped products are not available for order
            if (product.ProductType == ProductType.GroupedProduct)
            {
                currentWarnings.Add(T("ShoppingCart.ProductNotAvailableForOrder"));
            }

            // Validate product bundle, no customer entered price allowed
            if (product.ProductType == ProductType.BundledProduct
                && product.BundlePerItemPricing
                && cartItem.CustomerEnteredPrice != decimal.Zero)
            {
                currentWarnings.Add(T("ShoppingCart.Bundle.NoCustomerEnteredPrice"));
            }

            // Not published or no permissions for customer or store
            if (!product.Published
                || !await _aclService.AuthorizeAsync(product, cartItem.Customer)
                || !await _storeMappingService.AuthorizeAsync(product, storeId ?? _storeContext.CurrentStore.Id))
            {
                currentWarnings.Add(T("ShoppingCart.ProductUnpublished"));
            }

            // Disabled buy button
            if (cartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && product.DisableBuyButton)
            {
                currentWarnings.Add(T("ShoppingCart.BuyingDisabled"));
            }

            // Disabled wishlist button
            if (cartItem.ShoppingCartType == ShoppingCartType.Wishlist && product.DisableWishlistButton)
            {
                currentWarnings.Add(T("ShoppingCart.WishlistDisabled"));
            }

            // Call for price
            if (cartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && product.CallForPrice)
            {
                currentWarnings.Add(T("Products.CallForPrice"));
            }

            // Customer entered price
            if (product.CustomerEntersPrice &&
                (cartItem.CustomerEnteredPrice < product.MinimumCustomerEnteredPrice
                || cartItem.CustomerEnteredPrice > product.MaximumCustomerEnteredPrice))
            {
                var min = _currencyService.ConvertToWorkingCurrency(product.MinimumCustomerEnteredPrice);
                var max = _currencyService.ConvertToWorkingCurrency(product.MaximumCustomerEnteredPrice);

                currentWarnings.Add(T("ShoppingCart.CustomerEnteredPrice.RangeError", min, max));
            }

            var quanitityToValidate = quantity ?? cartItem.Quantity;
            // Quantity validation
            if (quanitityToValidate <= 0)
            {
                currentWarnings.Add(T("ShoppingCart.QuantityShouldPositive"));
            }

            if (quanitityToValidate < product.OrderMinimumQuantity)
            {
                currentWarnings.Add(T("ShoppingCart.MinimumQuantity", product.OrderMinimumQuantity));
            }

            if (quanitityToValidate > product.OrderMaximumQuantity)
            {
                currentWarnings.Add(T("ShoppingCart.MaximumQuantity", product.OrderMaximumQuantity));
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(quanitityToValidate))
            {
                currentWarnings.Add(T("ShoppingCart.AllowedQuantities", string.Join(", ", allowedQuantities)));
            }

            // Stock validation
            var validateOutOfStock = cartItem.ShoppingCartType == ShoppingCartType.ShoppingCart || !_cartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
            if (validateOutOfStock)
            {
                switch (product.ManageInventoryMethod)
                {
                    case ManageInventoryMethod.ManageStock:
                    {
                        if (product.BackorderMode != BackorderMode.NoBackorders || product.StockQuantity >= quanitityToValidate)
                            break;

                        var warning = product.StockQuantity > 0
                            ? T("ShoppingCart.QuantityExceedsStock", product.StockQuantity)
                            : T("ShoppingCart.OutOfStock");

                        currentWarnings.Add(warning);
                    }
                    break;
                    case ManageInventoryMethod.ManageStockByAttributes:
                    {
                        var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, cartItem.AttributeSelection);
                        if (combination == null || combination.AllowOutOfStockOrders || combination.StockQuantity >= quanitityToValidate)
                            break;

                        var warning = combination.StockQuantity > 0
                            ? T("ShoppingCart.QuantityExceedsStock", combination.StockQuantity)
                            : T("ShoppingCart.OutOfStock");

                        currentWarnings.Add(warning);
                    }
                    break;
                    case ManageInventoryMethod.DontManageStock:
                    default:
                        break;
                }
            }

            // Validate availability
            var availableStartDateError = false;
            if (product.AvailableStartDateTimeUtc.HasValue)
            {
                var availableStartDate = DateTime.SpecifyKind(product.AvailableStartDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableStartDate.CompareTo(DateTime.UtcNow) > 0)
                {
                    currentWarnings.Add(T("ShoppingCart.NotAvailable"));
                    availableStartDateError = true;
                }
            }

            if (product.AvailableEndDateTimeUtc.HasValue && !availableStartDateError)
            {
                var availableEndDate = DateTime.SpecifyKind(product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableEndDate.CompareTo(DateTime.UtcNow) < 0)
                {
                    currentWarnings.Add(T("ShoppingCart.NotAvailable"));
                }
            }

            warnings.AddRange(currentWarnings);
            return !currentWarnings.Any();
        }

        public virtual async Task<bool> ValidateProductAttributesAsync(
            Product product,
            ProductVariantAttributeSelection selection,
            int storeId,
            IList<string> warnings,
            int quantity = 1,
            Customer customer = null,
            ProductBundleItem bundleItem = null,
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart,
            IEnumerable<OrganizedShoppingCartItem> cartItems = null)
        {
            customer ??= _workContext.CurrentCustomer;
            cartItems ??= Enumerable.Empty<OrganizedShoppingCartItem>();

            var currentWarnings = new List<string>();

            // Check if the product is a bundle. Since bundles have no attributes, the customer has nothing to select.
            if (product.ProductType == ProductType.BundledProduct ||
                (bundleItem?.BundleProduct != null && !bundleItem.BundleProduct.BundlePerItemPricing))
            {
                if (selection.AttributesMap.Any())
                {
                    warnings.Add(T("ShoppingCart.Bundle.NoAttributes"));
                }

                return true;
            }

            // Get selected product variant attributes and check for product errors.
            var selectedAttributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(selection);
            foreach (var attribute in selectedAttributes)
            {
                if (attribute.Product == null || attribute.Product.Id != product.Id)
                {
                    warnings.Add(T("ShoppingCart.AttributeError"));
                    return false;
                }
            }

            await _db.LoadCollectionAsync(product, x => x.ProductVariantAttributes, false, q => q.Include(x => x.ProductAttribute));

            // Get existing product variant attributes.
            foreach (var existingAttribute in product.ProductVariantAttributes)
            {
                if (!existingAttribute.IsRequired)
                {
                    continue;
                }

                var found = false;

                foreach (var attribute in selectedAttributes)
                {
                    if (attribute.Id == existingAttribute.Id)
                    {
                        var values = selection.GetAttributeValues(attribute.Id) ?? Enumerable.Empty<object>();
                        foreach (var value in values)
                        {
                            var strValue = value?.ToString().EmptyNull();
                            if (strValue.HasValue())
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }

                if (!found &&
                    (bundleItem?.FilterAttributes ?? false) &&
                    !bundleItem.AttributeFilters.Any(x => x.AttributeId == existingAttribute.ProductAttributeId))
                {
                    // Attribute is filtered out by bundle item. It cannot be selected by the customer.
                    found = true;
                }

                if (!found)
                {
                    var prompt = existingAttribute.TextPrompt.NullEmpty() ?? existingAttribute.ProductAttribute.GetLocalized(x => x.Name);
                    currentWarnings.Add(T("ShoppingCart.SelectAttribute", prompt));
                }
            }

            if (currentWarnings.Any())
            {
                warnings.AddRange(currentWarnings);
                return false;
            }

            // Checks whether there is an active selected attribute combination.
            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, selection);
            if (combination != null && !combination.IsActive)
            {
                currentWarnings.Add(T("ShoppingCart.NotAvailable"));
            }

            var attributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(selection);

            var productLinkageValues = attributeValues
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                .ToArray();

            var linkedProductIds = productLinkageValues
                .Select(x => x.LinkedProductId)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var linkedProducts = linkedProductIds.Any()
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : new Dictionary<int, Product>();

            // Validate each linked product, create shopping cart item from linked product and run validation.
            foreach (var value in productLinkageValues)
            {
                if (linkedProducts.TryGetValue(value.LinkedProductId, out var linkedProduct))
                {
                    var item = new ShoppingCartItem
                    {
                        ProductId = linkedProduct.Id,
                        Product = linkedProduct,
                        ShoppingCartType = cartType,
                        Customer = customer,
                        StoreId = storeId,
                        Quantity = quantity * value.Quantity
                    };

                    var ctx = new AddToCartContext
                    {
                        Product = linkedProduct,
                        Customer = customer,
                        CartType = cartType,
                        StoreId = storeId,
                        Quantity = quantity * value.Quantity
                    };

                    // Get product linkage warnings.
                    await ValidateAddToCartItemAsync(ctx, item, cartItems);

                    foreach (var linkageWarning in ctx.Warnings)
                    {
                        currentWarnings.Add(T("ShoppingCart.ProductLinkageAttributeWarning",
                            value.ProductVariantAttribute.ProductAttribute.GetLocalized(x => x.Name),
                            value.GetLocalized(x => x.Name),
                            linkageWarning));
                    }
                }
                else
                {
                    currentWarnings.Add(T("ShoppingCart.ProductLinkageProductNotLoading", value.LinkedProductId));
                }
            }

            warnings.AddRange(currentWarnings);
            return !currentWarnings.Any();
        }

        public virtual async Task<bool> ValidateRequiredProductsAsync(Product product, IEnumerable<OrganizedShoppingCartItem> cartItems, IList<string> warnings)
        {
            Guard.NotNull(product, nameof(product));

            if (!product.RequireOtherProducts)
                return true;

            var requiredProductIds = product.ParseRequiredProductIds();
            if (!requiredProductIds.Any())
                return true;

            var cartProductIds = cartItems.Select(x => x.Item.ProductId);

            var missingRequiredProductIds = requiredProductIds.Except(cartProductIds);
            if (!missingRequiredProductIds.Any())
                return true;

            var isValid = true;
            var missingRequiredProducts = await _db.Products.GetManyAsync(missingRequiredProductIds);

            foreach (var requiredProduct in missingRequiredProducts)
            {
                isValid = false;
                warnings.Add(T("ShoppingCart.RequiredProductWarning", requiredProduct.GetLocalized(x => x.Name)));
            }

            return isValid;
        }
    }
}