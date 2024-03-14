using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Events;

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
        private readonly IRuleProviderFactory _ruleProviderFactory;
        private readonly IEventPublisher _eventPublisher;

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
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            IRuleProviderFactory ruleProviderFactory,
            IEventPublisher eventPublisher)
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
            _ruleProviderFactory = ruleProviderFactory;
            _eventPublisher = eventPublisher;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<bool> ValidateAccessPermissionsAsync(Customer customer, ShoppingCartType cartType, IList<string> warnings)
        {
            Guard.NotNull(customer);
            Guard.NotNull(warnings);

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
            Guard.NotNull(bundleItem);
            Guard.NotNull(warnings);

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

            return currentWarnings.Count == 0;
        }

        public virtual async Task<bool> ValidateCartAsync(ShoppingCart cart, IList<string> warnings, bool validateCheckoutAttributes = false)
        {
            Guard.NotNull(cart);
            Guard.NotNull(warnings);

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
                    if (!cart.IsShippingRequired)
                    {
                        // No shipping required. Filter attributes which require shippable products.
                        existingAttributesQuery = existingAttributesQuery.Where(x => !x.ShippableProductRequired);
                    }

                    var selectedAttributes = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(checkoutAttributes);
                    var notSelectedAttributes = await existingAttributesQuery
                        .Where(x => x.IsRequired && x.IsActive && !selectedAttributes.Contains(x))
                        .ToListAsync();

                    // Check for required attributes.
                    foreach (var attribute in notSelectedAttributes)
                    {
                        currentWarnings.Add(GetAttributeRequiredWarning(
                            attribute.AttributeControlType,
                            attribute.GetLocalized(x => x.TextPrompt).Value.NullEmpty() ?? attribute.GetLocalized(x => x.Name)));
                    }
                }
            }

            var validatingCartEvent = new ValidatingCartEvent(cart, warnings);
            await _eventPublisher.PublishAsync(validatingCartEvent);

            warnings.AddRange(currentWarnings);

            return warnings.Count == 0;
        }

        public virtual async Task<bool> ValidateAddToCartItemAsync(AddToCartContext ctx, ShoppingCartItem cartItem, IEnumerable<OrganizedShoppingCartItem> cartItems)
        {
            Guard.NotNull(ctx);
            Guard.NotNull(cartItem);
            Guard.NotNull(cartItems);

            var warnings = new List<string>();

            await ValidateProductAsync(cartItem, cartItems, warnings, ctx.StoreId);
            await this.ValidateProductAttributesAsync(cartItem, cartItems, warnings);

            ValidateGiftCardInfo(cartItem.Product, cartItem.AttributeSelection, warnings);

            // Bundle and bundle items (child items) warnings.
            if (ctx.BundleItem != null || !ctx.ChildItems.IsNullOrEmpty())
            {
                var bundleItem = ctx.BundleItem ?? ctx.ChildItems.Select(x => x.BundleItem).FirstOrDefault();
                if (bundleItem != null)
                {
                    ValidateBundleItem(bundleItem, warnings);
                }
            }

            ctx.Warnings.AddRange(warnings);

            return warnings.Count == 0;
        }

        public virtual bool ValidateItemsMaximumCartQuantity(ShoppingCartType cartType, int cartItemsCount, IList<string> warnings)
        {
            Guard.NotNull(warnings);

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
            Guard.NotNull(product);
            Guard.NotNull(selection);
            Guard.NotNull(warnings);

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

            return currentWarnings.Count == 0;
        }

        public virtual async Task<bool> ValidateProductAsync(
            ShoppingCartItem cartItem, 
            IEnumerable<OrganizedShoppingCartItem> cartItems,
            IList<string> warnings,
            int? storeId = null, 
            int? quantity = null)
        {
            Guard.NotNull(cartItem);
            Guard.NotNull(warnings);

            var p = cartItem.Product;
            if (p == null)
            {
                warnings.Add(T("Products.NotFound", cartItem.ProductId));
                return false;
            }

            if (p.Deleted)
            {
                warnings.Add(T("ShoppingCart.ProductDeleted"));
                return false;
            }

            var currentWarnings = new List<string>();

            // Grouped products are not available for order
            if (p.ProductType == ProductType.GroupedProduct)
            {
                currentWarnings.Add(T("ShoppingCart.ProductNotAvailableForOrder"));
            }

            // Validate product bundle, no customer entered price allowed
            if (p.ProductType == ProductType.BundledProduct
                && p.BundlePerItemPricing
                && cartItem.CustomerEnteredPrice != decimal.Zero)
            {
                currentWarnings.Add(T("ShoppingCart.Bundle.NoCustomerEnteredPrice"));
            }

            // Not published or no permissions for customer or store
            if (!p.Published
                || !await _aclService.AuthorizeAsync(p, cartItem.Customer)
                || !await _storeMappingService.AuthorizeAsync(p, storeId ?? _storeContext.CurrentStore.Id))
            {
                currentWarnings.Add(T("ShoppingCart.ProductUnpublished"));
            }

            // Disabled buy button
            if (cartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && p.DisableBuyButton)
            {
                currentWarnings.Add(T("ShoppingCart.BuyingDisabled"));
            }

            // Disabled wishlist button
            if (cartItem.ShoppingCartType == ShoppingCartType.Wishlist && p.DisableWishlistButton)
            {
                currentWarnings.Add(T("ShoppingCart.WishlistDisabled"));
            }

            // Call for price
            if (cartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && p.CallForPrice)
            {
                currentWarnings.Add(T("Products.CallForPrice"));
            }

            // Customer entered price
            if (p.CustomerEntersPrice &&
                (cartItem.CustomerEnteredPrice < p.MinimumCustomerEnteredPrice
                || cartItem.CustomerEnteredPrice > p.MaximumCustomerEnteredPrice))
            {
                var min = _currencyService.ConvertToWorkingCurrency(p.MinimumCustomerEnteredPrice);
                var max = _currencyService.ConvertToWorkingCurrency(p.MaximumCustomerEnteredPrice);

                currentWarnings.Add(T("ShoppingCart.CustomerEnteredPrice.RangeError", min, max));
            }

            // Quantity validation
            var hasQuantityWarnings = false;
            var quantityToValidate = quantity ?? cartItem.Quantity;
            if (quantityToValidate <= 0)
            {
                currentWarnings.Add(T("ShoppingCart.QuantityShouldPositive"));
                hasQuantityWarnings = true;
            }

            var allowedQuantities = p.ParseAllowedQuantities();

            var minQty = Math.Max(1, allowedQuantities.Length > 0 ? allowedQuantities[0] : p.OrderMinimumQuantity);
            if (quantityToValidate < minQty)
            {
                currentWarnings.Add(T("ShoppingCart.MinimumQuantity", minQty));
                hasQuantityWarnings = true;
            }

            var maxQty = Math.Max(minQty, allowedQuantities.Length > 0 ? allowedQuantities.Last() : p.OrderMaximumQuantity);
            if (quantityToValidate > maxQty)
            {
                currentWarnings.Add(T("ShoppingCart.MaximumQuantity", maxQty));
                hasQuantityWarnings = true;
            }

            if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(quantityToValidate))
            {
                currentWarnings.Add(T("ShoppingCart.AllowedQuantities", string.Join(", ", allowedQuantities)));
            }

            // Stock validation
            var validateStock = cartItem.ShoppingCartType == ShoppingCartType.ShoppingCart || !_cartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
            if (validateStock && !hasQuantityWarnings)
            {
                if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStock && p.BackorderMode == BackorderMode.NoBackorders)
                {
                    // INFO: bundles with per-item-pricing are always added in single positions (see ShoppingCart.FindItemInCart).
                    if (cartItems != null
                        && (_cartSettings.AddProductsToBasketInSinglePositions || (p.ProductType == ProductType.BundledProduct && p.BundlePerItemPricing)))
                    {
                        // Since products are added in single positions, we need to sum up the quantities of all items with the same product id
                        // except the current item of which we already have the qantity.
                        quantityToValidate += cartItems
                            .Select(x => x.Item)
                            .Where(x => x.ProductId == p.Id && x.ParentItemId == null && x.Id != cartItem.Id)
                            .Sum(x => x.Quantity);
                    }

                    if (p.StockQuantity < quantityToValidate)
                    {
                        currentWarnings.Add(p.StockQuantity > 0
                            ? T("ShoppingCart.QuantityExceedsStock", p.StockQuantity)
                            : T("ShoppingCart.OutOfStock"));
                    }
                }
                else if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
                {
                    var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(p.Id, cartItem.AttributeSelection);
                    if (combination != null && !combination.AllowOutOfStockOrders)
                    {
                        if (cartItems != null && _cartSettings.AddProductsToBasketInSinglePositions)
                        {
                            // Since products are added in single positions, we need to sum up the quantities of all items with the same product id.
                            // except the current item of which we already have the qantity.
                            quantityToValidate += cartItems
                                .Select(x => x.Item)
                                .Where(x => x.ProductId == p.Id && x.ParentItemId == null && x.Id != cartItem.Id && x.AttributeSelection.Equals(cartItem.AttributeSelection))
                                .Sum(x => x.Quantity);
                        }

                        if (combination.StockQuantity < quantityToValidate)
                        {
                            currentWarnings.Add(combination.StockQuantity > 0
                                ? T("ShoppingCart.QuantityExceedsStock", combination.StockQuantity)
                                : T("ShoppingCart.OutOfStock"));
                        }
                    }
                }
            }

            // Validate availability
            var invalidStartDate = false;
            if (p.AvailableStartDateTimeUtc.HasValue)
            {
                var availableStartDate = DateTime.SpecifyKind(p.AvailableStartDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableStartDate.CompareTo(DateTime.UtcNow) > 0)
                {
                    currentWarnings.Add(T("ShoppingCart.NotAvailable"));
                    invalidStartDate = true;
                }
            }

            if (p.AvailableEndDateTimeUtc.HasValue && !invalidStartDate)
            {
                var availableEndDate = DateTime.SpecifyKind(p.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableEndDate.CompareTo(DateTime.UtcNow) < 0)
                {
                    currentWarnings.Add(T("ShoppingCart.NotAvailable"));
                }
            }

            warnings.AddRange(currentWarnings);

            return currentWarnings.Count == 0;
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

            // Check if the product is a bundle. Since bundles have no attributes, the customer has nothing to select.
            if (product.ProductType == ProductType.BundledProduct ||
                (bundleItem?.BundleProduct != null && !bundleItem.BundleProduct.BundlePerItemPricing))
            {
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

            // Checks whether there is an active selected attribute combination.
            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, selection);
            if ((combination != null && !combination.IsActive) ||
                (product.AttributeCombinationRequired && combination == null))
            {
                warnings.Add(T("ShoppingCart.NotAvailable"));
                return false;
            }

            // Check product variant attributes.
            await _db.LoadCollectionAsync(product, x => x.ProductVariantAttributes, false, q => q
                .Include(x => x.ProductAttribute)
                .Include(x => x.ProductVariantAttributeValues)
                .Include(x => x.RuleSet));

            var currentWarnings = new List<string>();
            var selectedValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(selection);
            var ruleProvider = _ruleProviderFactory.GetProvider<IAttributeRuleProvider>(RuleScope.ProductAttribute, new AttributeRuleProviderContext(product.Id));

            foreach (var attribute in product.ProductVariantAttributes)
            {
                if (!attribute.IsRequired || !await ruleProvider.IsAttributeActiveAsync(new(product, attribute, selectedValues)))
                {
                    continue;
                }

                var found = false;

                foreach (var pva in selectedAttributes)
                {
                    if (pva.Id == attribute.Id)
                    {
                        var values = selection.GetAttributeValues(pva.Id) ?? Enumerable.Empty<object>();
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
                    bundleItem != null &&
                    bundleItem.FilterAttributes &&
                    !bundleItem.AttributeFilters.Any(x => x.AttributeId == attribute.Id))
                {
                    // Attribute is filtered out by bundle item. It cannot be selected by the customer.
                    found = true;
                }

                if (!found)
                {
                    currentWarnings.Add(GetAttributeRequiredWarning(
                        attribute.AttributeControlType,
                        attribute.TextPrompt.NullEmpty() ?? attribute.ProductAttribute.GetLocalized(x => x.Name)));
                }
            }

            if (currentWarnings.Count > 0)
            {
                warnings.AddRange(currentWarnings);
                return false;
            }

            var productLinkageValues = selectedValues
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                .ToArray();

            var linkedProductIds = productLinkageValues
                .Select(x => x.LinkedProductId)
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            var linkedProducts = linkedProductIds.Length > 0
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : [];

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

            return currentWarnings.Count == 0;
        }

        public virtual async Task<bool> ValidateRequiredProductsAsync(Product product, IEnumerable<OrganizedShoppingCartItem> cartItems, IList<string> warnings)
        {
            Guard.NotNull(product);

            if (!product.RequireOtherProducts)
                return true;

            var requiredProductIds = product.ParseRequiredProductIds();
            if (requiredProductIds.Length == 0)
                return true;

            var cartProductIds = cartItems.Select(x => x.Item.Product.Id);

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

        private string GetAttributeRequiredWarning(AttributeControlType type, string textPrompt)
        {
            switch (type)
            {
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                case AttributeControlType.Datepicker:
                    return T("ShoppingCart.EnterAttributeValue", textPrompt);

                case AttributeControlType.FileUpload:
                    return T("ShoppingCart.UploadAttributeFile", textPrompt);

                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.Checkboxes:
                case AttributeControlType.Boxes:
                default:
                    return T("ShoppingCart.SelectAttribute", textPrompt);
            }
        }
    }
}