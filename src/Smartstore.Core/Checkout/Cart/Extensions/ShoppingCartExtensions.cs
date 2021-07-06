using System;
using System.Linq;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Checkout.Cart
{
    public static partial class ShoppingCartExtensions
    {
        /// <summary>
        /// Finds a shopping cart item in cart.
        /// </summary>
        /// <remarks>Products with the same identifier need to have matching attribute selections as well.</remarks>
        /// <param name="cart">Shopping cart to search.</param>
        /// <param name="shoppingCartType">Shopping cart type to search.</param>
        /// <param name="product">Product to search for.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <param name="customerEnteredPrice">Customers entered price needs to match (if enabled by product).</param>
        /// <returns>Matching <see cref="OrganizedShoppingCartItem"/> or <c>null</c> if none was found.</returns>
        public static OrganizedShoppingCartItem FindItemInCart(
            this ShoppingCart cart,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection = null,
            Money? customerEnteredPrice = null)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(product, nameof(product));

            // Return on product bundle with individual item pricing - too complex
            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
            {
                return null;
            }

            // Filter non group items from correct cart type, with matching product id and product type id
            var filteredCart = cart.Items.Where(x => x.Item.ShoppingCartType == shoppingCartType &&
                x.Item.ParentItemId == null && 
                x.Item.Product.ProductTypeId == product.ProductTypeId &&
                x.Item.ProductId == product.Id);

            // There could be multiple matching products with the same identifier but different attributes/selections (etc).
            // Ensure matching product infos are the same (attributes, gift card values (if it is gift card), customerEnteredPrice).
            foreach (var cartItem in filteredCart)
            {
                var currentProduct = cartItem.Item.Product;

                // Compare attribute selection, if not null.
                if (selection != null && selection.AttributesMap.Any())
                {
                    var cartItemSelection = cartItem.Item.AttributeSelection;
                    if (!cartItemSelection.Equals(selection))
                    {
                        continue;
                    }

                    // Compare gift cards info values (if it is a gift card).
                    if (currentProduct.IsGiftCard && 
                        (cartItemSelection.GiftCardInfo == null || selection.GiftCardInfo == null || cartItemSelection != selection))
                    {
                        continue;
                    }
                }

                // Products with CustomerEntersPrice are equal if the price is the same.
                // But a system product may only be placed once in the shopping cart.
                if (customerEnteredPrice.HasValue)
                {
                    var enteredPrice = customerEnteredPrice.Value;

                    if (currentProduct.CustomerEntersPrice && !currentProduct.IsSystemProduct &&
                        enteredPrice != decimal.Round(cartItem.Item.CustomerEnteredPrice, enteredPrice.DecimalDigits))
                    {
                        continue;
                    }
                }

                // If we got this far, we found a matching product with the same values.
                return cartItem;
            }

            return null;
        }

        /// <summary>
        /// Checks whether the shopping cart requires shipping.
        /// </summary>
        /// <returns>
        /// <c>True</c> if any product requires shipping, otherwise <c>false</c>.
        /// </returns>
        public static bool IsShippingRequired(this ShoppingCart cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Where(x => x.Item.IsShippingEnabled).Any();
        }

        /// <summary>
        /// Gets the total quantity of products in the cart.
        /// </summary>
		public static int GetTotalQuantity(this ShoppingCart cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Sum(x => x.Item.Quantity);
        }

        /// <summary>
        /// Gets a value indicating whether the cart includes products matching the condition.
        /// </summary>
        /// <param name="matcher">The condition to match cart items.</param>
        /// <returns><c>True</c> if any product matches the condition, otherwise <c>false</c>.</returns>
        public static bool IncludesMatchingItems(this ShoppingCart cart, Func<Product, bool> matcher)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Where(x => x.Item.Product != null && matcher(x.Item.Product)).Any();
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart contains a recurring item.
        /// </summary>
        /// <returns>A value indicating whether the shopping cart contains a recurring item.</returns>
		public static bool ContainsRecurringItem(this ShoppingCart cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Where(x => x.Item.Product?.IsRecurring ?? false).Any();
        }

        /// <summary>
        /// Gets the recurring cycle information.
        /// </summary>
        /// <returns><see cref="RecurringCycleInfo"/>.</returns>
        public static RecurringCycleInfo GetRecurringCycleInfo(this ShoppingCart cart, ILocalizationService localizationService)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(localizationService, nameof(localizationService));

            var info = new RecurringCycleInfo();
            int? cycleLength = null;
            RecurringProductCyclePeriod? cyclePeriod = null;
            int? totalCycles = null;

            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Item.Product;
                if (product == null)
                {
                    throw new SmartException($"Product with Id={cartItem.Item.ProductId} cannot be loaded.");
                }

                if (product.IsRecurring)
                {
                    if (cycleLength.HasValue && cycleLength.Value != product.RecurringCycleLength)
                    {
                        info.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                        return info;
                    }
                    else
                    {
                        cycleLength = product.RecurringCycleLength;
                    }

                    if (cyclePeriod.HasValue && cyclePeriod.Value != product.RecurringCyclePeriod)
                    {
                        info.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                        return info;
                    }
                    else
                    {
                        cyclePeriod = product.RecurringCyclePeriod;
                    }

                    if (totalCycles.HasValue && totalCycles.Value != product.RecurringTotalCycles)
                    {
                        info.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                        return info;
                    }
                    else
                    {
                        totalCycles = product.RecurringTotalCycles;
                    }
                }
            }

            if (cycleLength.HasValue && cyclePeriod.HasValue && totalCycles.HasValue)
            {
                info.CycleLength = cycleLength.Value;
                info.CyclePeriod = cyclePeriod.Value;
                info.TotalCycles = totalCycles.Value;
            }

            return info;
        }
    }
}
