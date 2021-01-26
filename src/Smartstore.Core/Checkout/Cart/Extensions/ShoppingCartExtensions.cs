using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Customers;
using Smartstore.Core.Localization;

namespace Smartstore
{
    /// <summary>
    /// Shopping cart extension methods
    /// </summary>
    public static class ShoppingCartExtensions
    {
        /// <summary>
        /// Finds and returns first matching product from shopping cart.
        /// </summary>
        /// <remarks>
        /// Products with the same identifier need to have matching attribute selections as well.
        /// </remarks>
        /// <param name="cart"></param>
        /// <param name="shoppingCartType"></param>
        /// <param name="product"></param>
        /// <param name="selection"></param>
        /// <param name="customerEnteredPrice"></param>
        /// <returns>Matching <see cref="OrganizedShoppingCartItem"/> or <c>null</c> if none was found.</returns>
        public static OrganizedShoppingCartItem FindItemInCart(
            this IList<OrganizedShoppingCartItem> cart,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection,
            decimal customerEnteredPrice = decimal.Zero)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(product, nameof(product));

            // Return on product bundle with individual item pricing - too complex
            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
                return null;

            // Filter non group items from correct cart type, with matching product id and product type id
            var filteredCart = cart
                .Where(x => x.Item.ShoppingCartType == shoppingCartType
                && x.Item.ParentItemId == null
                && x.Item.Product.ProductTypeId == product.ProductTypeId
                && x.Item.ProductId == product.Id);

            // There could be multiple matching products with the same identifier but different attributes/selections (etc).
            // Ensure matching product infos are the same (attributes, gift card values (if it is one), customerEnteredPrice).
            foreach (var cartItem in filteredCart)
            {
                // Compare attribute selection
                var cartItemSelection = new ProductVariantAttributeSelection(cartItem.Item.RawAttributes);
                if (cartItemSelection != selection)
                    continue;

                var currentProduct = cartItem.Item.Product;

                // Compare gift cards info values (if it is a gift card)
                if (currentProduct.IsGiftCard &&
                    (cartItemSelection.GiftCardInfo == null
                    || selection.GiftCardInfo == null
                    || cartItemSelection != selection))
                {
                    continue;
                }

                // Products with CustomerEntersPrice are equal if the price is the same.
                // But a system product may only be placed once in the shopping cart.
                if (currentProduct.CustomerEntersPrice && !currentProduct.IsSystemProduct
                    && Math.Round(cartItem.Item.CustomerEnteredPrice, 2) != Math.Round(customerEnteredPrice, 2))
                {
                    continue;
                }

                // If we got this far, we found a matching product with the same values
                return cartItem;
            }

            return null;
        }


        /// <summary>
        /// Checks whether the shopping cart requires shipping
        /// </summary>
        /// <returns>
        /// <c>true</c> if any product requires shipping
        /// </returns>
        public static bool IsShippingRequired(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => x.Item.IsShippingEnabled).Any();
        }

        /// <summary>
        /// Gets the total quantity of products in the cart
        /// </summary>
		public static int GetTotalQuantity(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Sum(x => x.Item.Quantity);
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart includes recurring products
        /// </summary>
        /// <returns>
        /// <c>true</c> if any product is recurring
        /// </returns>
		public static bool IncludesRecurringProducts(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => x.Item.Product?.IsRecurring ?? false).Any();
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart includes standard (not recurring) products
        /// </summary>
        /// <returns>
        /// <c>true</c> if any product is standard
        /// </returns>
        public static bool IncludesStandardProducts(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => !x.Item.Product?.IsRecurring ?? false).Any();
        }

        public static bool Includes(this IEnumerable<OrganizedShoppingCartItem> cart, Func<Product, bool> matcher)
        {
            Guard.NotNull(cart, nameof(cart));
            // TODO: (ms) (core) Find a better name for this.
            return cart.Where(x => x.Item.Product != null ? matcher(x.Item.Product) : false).Any();
        }

        /// <summary>
        /// Gets the recurring cycle information
        /// </summary>
		public static RecurringCycleInfo GetRecurringCycleInfo(this IEnumerable<OrganizedShoppingCartItem> cart, ILocalizationService localizationService)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(localizationService, nameof(localizationService));

            var cycleInfo = new RecurringCycleInfo();

            foreach (var organizedItem in cart)
            {
                var product = organizedItem.Item.Product;
                if (product is null)
                    throw new SmartException(string.Format("Product (Id={0}) cannot be loaded", organizedItem.Item.ProductId));

                if (!product.IsRecurring)
                    continue;

                if (!cycleInfo.HasValues)
                {
                    cycleInfo.CycleLength = product.RecurringCycleLength;
                    cycleInfo.CyclePeriod = product.RecurringCyclePeriod;
                    cycleInfo.TotalCycles = product.RecurringTotalCycles;
                    continue;
                }

                if (cycleInfo.CycleLength != product.RecurringCycleLength
                    || cycleInfo.CyclePeriod != product.RecurringCyclePeriod
                    || cycleInfo.CyclePeriod != product.RecurringCyclePeriod)
                {
                    cycleInfo.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                    break;
                }
            }

            return cycleInfo;
        }

        /// <summary>
        /// Gets customer of shopping cart
        /// </summary>
        /// <returns>
        /// <see cref="Customer"/> of <see cref="OrganizedShoppingCartItem"/> or <c>null</c> if cart is empty
        /// </returns>
        public static Customer GetCustomer(this IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Count > 0 ? cart[0].Item.Customer : null;
        }
    }
}
