using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Cart
{
    // TODO: (ms) (core) Media load flags warnings
    /// <summary>
    /// Shopping cart validation methods
    /// </summary>
    public partial class ShoppingCartValidator : IShoppingCartValidator
    {
        private readonly SmartDbContext _db;
        private readonly IAclService _aclService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IPriceFormatter _priceFormatter;
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
            IPriceFormatter priceFormatter,
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
            _priceFormatter = priceFormatter;
            _currencyService = currencyService;
            _cartSettings = cartSettings;
            _permissionService = permissionService;
            _storeMappingService = storeMappingService;
            _localizationService = localizationService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public virtual async Task<IList<string>> ValidateAccessPermissionsAsync(AddToCartContext ctx)
        {
            Guard.NotNull(ctx, nameof(ctx));

            var warnings = new List<string>();

            // Validate access permissions
            if (ctx.CartType == ShoppingCartType.ShoppingCart && !await _permissionService.AuthorizeAsync(Permissions.Cart.AccessShoppingCart, ctx.Customer))
            {
                warnings.Add(T("ShoppingCart.IsDisabled"));
            }
            else if (ctx.CartType == ShoppingCartType.Wishlist && !await _permissionService.AuthorizeAsync(Permissions.Cart.AccessWishlist, ctx.Customer))
            {
                warnings.Add(T("Wishlist.IsDisabled"));
            }

            ctx.Warnings.AddRange(warnings);
            return warnings;
        }

        // was GetBundleItemWarnings
        public virtual IList<string> ValidateBundleItems(IEnumerable<ProductBundleItem> bundleItems)
        {
            Guard.NotNull(bundleItems, nameof(bundleItems));

            var warnings = new List<string>();

            foreach (var bundleItem in bundleItems)
            {
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
            }

            return warnings;
        }

        // was GetShoppingCartWarnings
        public virtual async Task<IList<string>> ValidateCartAsync(
            IEnumerable<OrganizedShoppingCartItem> cartItems,
            CheckoutAttributeSelection attributeSelection,
            bool validateCheckoutAttributes)
        {
            Guard.NotNull(attributeSelection, nameof(attributeSelection));
            Guard.NotNull(cartItems, nameof(cartItems));

            var warnings = new List<string>();

            var missingProduct = cartItems.Where(x => x.Item.Product is null).FirstOrDefault();
            if (missingProduct != null)
            {
                warnings.Add(T("ShoppingCart.CannotLoadProduct", missingProduct.Item.ProductId));
                return warnings;
            }

            var hasNonRecurringProducts = cartItems.IncludesMatchingItems(x => !x.IsRecurring);
            var hasRecurringProducts = cartItems.IncludesMatchingItems(x => x.IsRecurring);

            // Do not mix recurring and non-recurring products
            if (hasNonRecurringProducts && hasRecurringProducts)
            {
                warnings.Add(T("ShoppingCart.CannotMixStandardAndAutoshipProducts"));
            }

            // Validate recurring products
            if (hasRecurringProducts)
            {
                var recurringCycleInfo = cartItems.GetRecurringCycleInfo(_localizationService);
                if (recurringCycleInfo.ErrorMessage.HasValue())
                {
                    warnings.Add(recurringCycleInfo.ErrorMessage);
                    return warnings;
                }
            }

            if (validateCheckoutAttributes)
            {
                var existingAttributesQuery = _db.CheckoutAttributes.ApplyStoreFilter(_storeContext.CurrentStore.Id);
                if (!cartItems.IsShippingRequired())
                {
                    // No shipping required. Filter attributes which require shippable products
                    existingAttributesQuery = existingAttributesQuery.Where(x => !x.ShippableProductRequired);
                }

                var selectedAttributes = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributesAsync(attributeSelection);
                var notSelectedAttributes = await existingAttributesQuery
                    .Where(x => x.IsRequired)
                    .Except(selectedAttributes)
                    .ToListAsync();

                // Check for not selected attributes
                foreach (var attribute in notSelectedAttributes)
                {
                    warnings.Add(T(
                        "ShoppingCart.SelectAttribute",
                        attribute.TextPrompt.IsEmpty()
                            ? attribute.GetLocalized(x => x.Name)
                            : attribute.GetLocalized(x => x.TextPrompt)
                        ));
                }
            }

            return warnings;
        }

        // was GetShoppingCartItemGiftCardWarnings
        public virtual IList<string> ValidateGiftCardInfo(AddToCartContext ctx)
        {
            Guard.NotNull(ctx, nameof(ctx));

            var warnings = new List<string>();

            if (!ctx.Product.IsGiftCard)
                return warnings;

            var recipientName = ctx.AttributeSelection.GiftCardInfo?.RecipientName;
            var recipientEmail = ctx.AttributeSelection.GiftCardInfo?.RecipientEmail;
            var senderName = ctx.AttributeSelection.GiftCardInfo?.SenderName;
            var senderEmail = ctx.AttributeSelection.GiftCardInfo?.SenderEmail;

            if (recipientName.IsEmpty())
            {
                warnings.Add(T("ShoppingCart.RecipientNameError"));
            }

            if (senderName.IsEmpty())
            {
                warnings.Add(T("ShoppingCart.SenderNameError"));
            }

            if (ctx.Product.GiftCardType == GiftCardType.Virtual)
            {
                if (recipientEmail.IsEmpty() || !recipientEmail.IsEmail())
                {
                    warnings.Add(T("ShoppingCart.RecipientEmailError"));
                }

                if (senderEmail.IsEmpty() || !senderEmail.IsEmail())
                {
                    warnings.Add(T("ShoppingCart.SenderEmailError"));
                }
            }

            ctx.Warnings.AddRange(warnings);
            return warnings;
        }

        // was GetShoppingCartItemWarnings
        // TODO: (ms) (core) Have bundle item, and child item already added to context
        public virtual async Task<IList<string>> ValidateCartItemAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart)
        {
            Guard.NotNull(ctx, nameof(ctx));
            Guard.NotNull(shoppingCart, nameof(shoppingCart));

            await ValidateProductAsync(ctx, shoppingCart);
            await ValidateProductAttributesAsync(ctx, shoppingCart);
            ValidateGiftCardInfo(ctx);
            ctx.Warnings.AddRange(await ValidateRequiredProductsAsync(ctx, shoppingCart));

            // Bundle and bundle item warnings
            if (ctx.BundleItem != null)
            {
                ctx.Warnings.AddRange(this.ValidateBundleItem(ctx.BundleItem));
            }

            if (ctx.ChildItems != null)
            {
                var childItems = ctx.ChildItems.Select(x => x.BundleItem).ToList();
                ctx.Warnings.AddRange(ValidateBundleItems(childItems));
            }

            return ctx.Warnings;
        }

        public virtual IList<string> ValidateCartItemsMaximum(ShoppingCartType cartType, int cartItemsCount)
        {
            var warnings = new List<string>();

            if (cartType == ShoppingCartType.ShoppingCart && cartItemsCount >= _cartSettings.MaximumShoppingCartItems)
            {
                warnings.Add(T("ShoppingCart.MaximumShoppingCartItems"));
            }
            else if (cartType == ShoppingCartType.Wishlist && cartItemsCount >= _cartSettings.MaximumWishlistItems)
            {
                warnings.Add(T("ShoppingCart.MaximumWishlistItems"));
            }

            return warnings;
        }

        public virtual async Task<IList<string>> ValidateProductAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart)
        {
            Guard.NotNull(ctx, nameof(ctx));

            var warnings = new List<string>();
            var product = ctx.Product;
            var cartType = ctx.CartType;

            if (product.Deleted)
            {
                warnings.Add(T("ShoppingCart.ProductDeleted"));
                ctx.Warnings.AddRange(warnings);
                return ctx.Warnings;
            }

            // Grouped products are not available for order
            if (product.ProductType == ProductType.GroupedProduct)
            {
                warnings.Add(T("ShoppingCart.ProductNotAvailableForOrder"));
            }

            // Validate product bundle, no customer entered price allowed
            if (product.ProductType == ProductType.BundledProduct
                && product.BundlePerItemPricing
                && ctx.CustomerEnteredPrice != decimal.Zero)
            {
                warnings.Add(T("ShoppingCart.Bundle.NoCustomerEnteredPrice"));
            }

            // Not published or no permissions for customer or store
            if (!product.Published
                || !await _aclService.AuthorizeAsync(product, ctx.Customer)
                || !await _storeMappingService.AuthorizeAsync(product.Name, product.Id, ctx.StoreId ?? _storeContext.CurrentStore.Id))
            {
                warnings.Add(T("ShoppingCart.ProductUnpublished"));
            }

            // Disabled buy button
            if (cartType == ShoppingCartType.ShoppingCart && product.DisableBuyButton)
            {
                warnings.Add(T("ShoppingCart.BuyingDisabled"));
            }

            // Disabled wishlist button
            if (cartType == ShoppingCartType.Wishlist && product.DisableWishlistButton)
            {
                warnings.Add(T("ShoppingCart.WishlistDisabled"));
            }

            // Call for price
            if (cartType == ShoppingCartType.ShoppingCart && product.CallForPrice)
            {
                warnings.Add(T("Products.CallForPrice"));
            }

            // Customer entered price
            if (product.CustomerEntersPrice
                && (ctx.CustomerEnteredPrice < product.MinimumCustomerEnteredPrice
                || ctx.CustomerEnteredPrice > product.MaximumCustomerEnteredPrice))
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
            if (ctx.Quantity <= 0)
            {
                warnings.Add(T("ShoppingCart.QuantityShouldPositive"));
            }

            if (ctx.Quantity < product.OrderMinimumQuantity)
            {
                warnings.Add(T("ShoppingCart.MinimumQuantity", product.OrderMinimumQuantity));
            }

            if (ctx.Quantity > product.OrderMaximumQuantity)
            {
                warnings.Add(T("ShoppingCart.MaximumQuantity", product.OrderMaximumQuantity));
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(ctx.Quantity))
            {
                warnings.Add(T("ShoppingCart.AllowedQuantities", string.Join(", ", allowedQuantities)));
            }

            // Stock validation
            var validateOutOfStock = ctx.CartType == ShoppingCartType.ShoppingCart || !_cartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
            if (validateOutOfStock)
            {
                switch (product.ManageInventoryMethod)
                {
                    case ManageInventoryMethod.ManageStock:
                        {
                            if (product.BackorderMode != BackorderMode.NoBackorders || product.StockQuantity >= ctx.Quantity)
                                break;

                            var warning = product.StockQuantity > 0
                                ? T("ShoppingCart.QuantityExceedsStock", product.StockQuantity)
                                : T("ShoppingCart.OutOfStock");

                            warnings.Add(warning);
                        }
                        break;
                    case ManageInventoryMethod.ManageStockByAttributes:
                        {
                            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, ctx.AttributeSelection);
                            if (combination == null || combination.AllowOutOfStockOrders || combination.StockQuantity >= ctx.Quantity)
                                break;

                            var warning = combination.StockQuantity > 0
                                ? T("ShoppingCart.QuantityExceedsStock", combination.StockQuantity)
                                : T("ShoppingCart.OutOfStock");

                            warnings.Add(warning);
                        }
                        break;
                    case ManageInventoryMethod.DontManageStock:
                    default:
                        break;
                }
            }

            // Validate availability
            var availableStartDateError = false;
            if (ctx.Product.AvailableStartDateTimeUtc.HasValue)
            {
                var availableStartDate = DateTime.SpecifyKind(ctx.Product.AvailableStartDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableStartDate.CompareTo(DateTime.UtcNow) > 0)
                {
                    warnings.Add(T("ShoppingCart.NotAvailable"));
                    availableStartDateError = true;
                }
            }

            if (ctx.Product.AvailableEndDateTimeUtc.HasValue && !availableStartDateError)
            {
                var availableEndDate = DateTime.SpecifyKind(ctx.Product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableEndDate.CompareTo(DateTime.UtcNow) < 0)
                {
                    warnings.Add(T("ShoppingCart.NotAvailable"));
                }
            }

            ctx.Warnings.AddRange(warnings);
            return warnings;
        }

        // was GetShoppingCartItemAttributeWarnings
        public virtual async Task<IList<string>> ValidateProductAttributesAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart)
        {
            Guard.NotNull(ctx, nameof(ctx));

            var warnings = new List<string>();

            // Customer cannot select anything cause bundles have no attributes
            if (ctx.Product.ProductType == ProductType.BundledProduct
                || ctx.BundleItem != null && !ctx.BundleItem.BundleProduct.BundlePerItemPricing)
                return warnings;

            // Get selected product variant attributes
            var selectedAttributes = await _productAttributeMaterializer.MaterializeProductVariantAttributesAsync(ctx.AttributeSelection);
            foreach (var attribute in selectedAttributes)
            {
                if (attribute.Product == null || attribute.Product.Id != ctx.Product.Id)
                {
                    warnings.Add(T("ShoppingCart.AttributeError"));
                    ctx.Warnings.AddRange(warnings);
                    return warnings;
                }
            }

            // Get existing product variant attributes
            foreach (var existingAttribute in ctx.Product.ProductVariantAttributes)
            {
                if (!existingAttribute.IsRequired)
                    continue;

                var found = false;
                // Selected product attributes
                foreach (var selectedAttribute in selectedAttributes)
                {
                    if (selectedAttribute.Id == existingAttribute.Id)
                    {
                        var values = ctx.AttributeSelection.GetAttributeValues(selectedAttribute.Id).Select(x => x.ToString()).ToList();
                        found = values.Find(x => x.HasValue()).HasValue();

                        if (found)
                            break;
                    }
                }

                // If attribute is filtered out by bundle item, it cannot be selected by the customer
                found = !found && (ctx.BundleItem?.FilterAttributes ?? false)
                    ? !ctx.BundleItem.AttributeFilters.Any(x => x.AttributeId == existingAttribute.ProductAttributeId)
                    : true;

                if (!found)
                {
                    warnings.Add(T(
                        "ShoppingCart.SelectAttribute",
                        existingAttribute.TextPrompt.IsEmpty()
                            ? existingAttribute.ProductAttribute.GetLocalized(x => x.Name)
                            : existingAttribute.GetLocalized(x => x.TextPrompt)
                        ));
                }
            }

            if (warnings.Count > 0)
            {
                ctx.Warnings.AddRange(warnings);
                return warnings;
            }

            // Checks whether there is an active selected attribute combination
            if (ctx.AttributeSelection.AttributesMap.Any())
            {
                var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(ctx.Product.Id, ctx.AttributeSelection);
                if (combination != null && !combination.IsActive)
                {
                    warnings.Add(T("ShoppingCart.NotAvailable"));
                }
            }

            var attributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(ctx.AttributeSelection);

            var linkedProductIds = attributeValues
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                .Select(x => x.LinkedProductId)
                .Distinct();

            // Get products linked to attributes
            var linkedProducts = await _db.Products.GetManyAsync(linkedProductIds);

            // Filter products which could not be loaded
            var notFoundProductIds = linkedProductIds.Except(linkedProducts.Select(x => x.Id));
            foreach (var productId in notFoundProductIds)
            {
                warnings.Add(T("ShoppingCart.ProductLinkageProductNotLoading", productId));
            }

            // Validate each linkedProduct, create shopping cart item from linkedProduct and run validation
            foreach (var attributeValue in attributeValues)
            {
                var linkedProduct = linkedProducts.FirstOrDefault(x => x.Id == attributeValue.LinkedProductId);
                if (linkedProduct == null)
                {
                    warnings.Add(T("ShoppingCart.ProductLinkageProductNotLoading", attributeValue.LinkedProductId));
                    continue;
                }

                var newCtx = new AddToCartContext
                {
                    Product = linkedProduct,
                    Customer = ctx.Customer,
                    CartType = ctx.CartType,
                    StoreId = ctx.StoreId,
                    Quantity = ctx.Quantity * attributeValue.Quantity
                };

                var linkageWarnings = await ValidateCartItemAsync(newCtx, shoppingCart);
                foreach (var linkageWarning in linkageWarnings)
                {
                    warnings.Add(
                        T("ShoppingCart.ProductLinkageAttributeWarning",
                            attributeValue.ProductVariantAttribute.ProductAttribute.GetLocalized(x => x.Name),
                            attributeValue.GetLocalized(x => x.Name),
                            linkageWarning)
                        );
                }
            }

            ctx.Warnings.AddRange(warnings);
            return warnings;
        }

        public virtual async Task<IList<string>> ValidateRequiredProductsAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> cartItems)
        {
            Guard.NotNull(cartItems, nameof(cartItems));
            Guard.NotNull(ctx, nameof(ctx));

            var warnings = new List<string>();

            if (!ctx.Product.RequireOtherProducts)
                return warnings;

            var requiredProductIds = ctx.Product.ParseRequiredProductIds();
            if (!requiredProductIds.Any())
                return warnings;

            var cartProductIds = cartItems.Select(x => x.Item.ProductId);

            var missingRequiredProductIds = requiredProductIds.Except(cartProductIds);
            if (!missingRequiredProductIds.Any())
                return warnings;

            var missingRequiredProducts = await _db.Products.GetManyAsync(missingRequiredProductIds);

            foreach (var product in missingRequiredProducts)
            {
                warnings.Add(T("ShoppingCart.RequiredProductWarning", product.GetLocalized(x => x.Name)));
            }

            ctx.Warnings.AddRange(warnings);
            return warnings;
        }
    }
}