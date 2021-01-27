using System;
using System.Collections.Generic;
using System.Linq;
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
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Cart
{
    public partial class ShoppingCartValidator : IShoppingCartValidator
    {
        private readonly SmartDbContext _db;
        private readonly IAclService _aclService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICurrencyService _currencyService;
        private readonly ShoppingCartSettings _cartSettings;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ILocalizationService _localizationService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        //private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        public ShoppingCartValidator(
            SmartDbContext db,
            IAclService aclService,
            IWorkContext workContext,
            IStoreContext storeContext,
            IPriceFormatter priceFormatter,
            ICurrencyService currencyService,
            ShoppingCartSettings cartSettings,
            IStoreMappingService storeMappingService,
            ILocalizationService localizationService,            
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer
            //,IOrderTotalCalculationService orderTotalCalculationService
            )
        {
            _db = db;
            _aclService = aclService;
            _workContext = workContext;
            _storeContext = storeContext;
            _priceFormatter = priceFormatter;
            _currencyService = currencyService;
            _cartSettings = cartSettings;
            _storeMappingService = storeMappingService;
            _localizationService = localizationService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            //_orderTotalCalculationService = orderTotalCalculationService;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public virtual async Task<List<string>> GetRequiredProductWarningsAsync(
            Customer customer,
            ShoppingCartType shoppingCartType,
            Product product,
            int storeId,
            bool automaticallyAddRequiredProducsIfEnabled)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(product, nameof(product));

            if (!product.RequireOtherProducts)
                return new();

            // TODO: Needs return values of GetCarItemsAsync and AddToCartAsync w/o accessing shopping cart service
            var cart = new List<OrganizedShoppingCartItem>(); //await GetCartItemsAsync(customer, shoppingCartType, storeId);

            var requiredProducts = await _db.Products.GetManyAsync(product.ParseRequiredProductIds());
            var productsNotInCart = requiredProducts.Where(x => !cart.Select(x => x.Item.ProductId).Contains(x.Id));

            var warnings = new List<string>();
            foreach (var currentProduct in productsNotInCart)
            {
                if (currentProduct.AutomaticallyAddRequiredProducts && automaticallyAddRequiredProducsIfEnabled)
                {
                    //// Pass 'false' for 'automaticallyAddRequiredProducsIfEnabled' to prevent circular references
                    //var addToCartWarnings = await AddToCartAsync(customer, currentProduct, shoppingCartType, storeId);
                    //if (addToCartWarnings.Count > 0)
                    //{
                    //    // A product could not be added for some reasons
                    //    // in this case, we display only generic errors
                    //    warnings.Add(T("ShoppingCart.RequiredProductWarning", currentProduct.GetLocalized(x => x.Name)));
                    //}
                }
                else
                {
                    warnings.Add(T("ShoppingCart.RequiredProductWarning", currentProduct.GetLocalized(x => x.Name)));
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
            var validateOutOfStock = shoppingCartType == ShoppingCartType.ShoppingCart || !_cartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
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
                        found = values.Find(x => x.HasValue()).HasValue();
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

            if (recipientName.IsEmpty())
            {
                warnings.Add(T("ShoppingCart.RecipientNameError"));
            }

            if (senderName.IsEmpty())
            {
                warnings.Add(T("ShoppingCart.SenderNameError"));
            }

            if (product.GiftCardType == GiftCardType.Virtual)
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
                if (!recurringCycleInfo.ErrorMessage.IsEmpty())
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

    }
}
