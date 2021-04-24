using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore
{
    /// <summary>
    /// Shopping cart extension methods
    /// </summary>
    public static class ShoppingCartItemExtensions
    {
        /// <summary>
        /// Finds and returns first matching product from shopping cart.
        /// </summary>
        /// <remarks>
        /// Products with the same identifier need to have matching attribute selections as well.
        /// </remarks>
        /// <param name="cart">Shopping cart to search in.</param>
        /// <param name="shoppingCartType">Shopping cart type to search in.</param>
        /// <param name="product">Product to search for.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <param name="customerEnteredPrice">Customers entered price needs to match (if enabled by product).</param>
        /// <returns>Matching <see cref="OrganizedShoppingCartItem"/> or <c>null</c> if none was found.</returns>
        public static OrganizedShoppingCartItem FindItemInCart(
            this IList<OrganizedShoppingCartItem> cart,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection = null,
            Money? customerEnteredPrice = null)
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
            // Ensure matching product infos are the same (attributes, gift card values (if it is gift card), customerEnteredPrice).
            foreach (var cartItem in filteredCart)
            {
                var currentProduct = cartItem.Item.Product;

                // Compare attribute selection, if not null
                if (selection != null)
                {
                    var cartItemSelection = cartItem.Item.AttributeSelection;
                    if (cartItemSelection != selection)
                    {
                        continue;
                    }

                    // Compare gift cards info values (if it is a gift card)
                    if (currentProduct.IsGiftCard &&
                        (cartItemSelection.GiftCardInfo == null
                        || selection.GiftCardInfo == null
                        || cartItemSelection != selection))
                    {
                        continue;
                    }
                }

                // Products with CustomerEntersPrice are equal if the price is the same.
                // But a system product may only be placed once in the shopping cart.
                if (customerEnteredPrice.HasValue)
                {
                    var enteredPrice = customerEnteredPrice.Value;

                    if (currentProduct.CustomerEntersPrice
                        && !currentProduct.IsSystemProduct
                        && enteredPrice != decimal.Round(cartItem.Item.CustomerEnteredPrice, enteredPrice.DecimalDigits))
                    {
                        continue;
                    }
                }

                // If we got this far, we found a matching product with the same values
                return cartItem;
            }

            return null;
        }

        /// <summary>
        /// Checks whether the shopping cart requires shipping.
        /// </summary>
        /// <returns>
        /// <c>True</c> if any product requires shipping; otherwise <c>false</c>.
        /// </returns>
        public static bool IsShippingRequired(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => x.Item.IsShippingEnabled).Any();
        }

        /// <summary>
        /// Gets the total quantity of products in the cart.
        /// </summary>
		public static int GetTotalQuantity(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Sum(x => x.Item.Quantity);
        }

        /// <summary>
        /// Gets a value indicating whether the cart includes products matching the condition.
        /// </summary>
        /// <param name="matcher">The condition to match cart items.</param>
        /// <returns>
        /// <c>True</c> if any product matches the condition; otherwise <c>false</c>.
        /// <see cref=""/>
        /// </returns>
        public static bool IncludesMatchingItems(this IEnumerable<OrganizedShoppingCartItem> cart, Func<Product, bool> matcher)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => x.Item.Product != null && matcher(x.Item.Product)).Any();
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart contains a recurring item.
        /// </summary>
        /// <param name="shoppingCart">Shopping cart.</param>
        /// <returns>A value indicating whether the shopping cart contains a recurring item.</returns>
		public static bool ContainsRecurringItem(this IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => x.Item.Product?.IsRecurring ?? false).Any();
        }

        /// <summary>
        /// Gets the recurring cycle information.
        /// <param name="localizationService">The localization service.</param>
        /// </summary>
        /// <returns>
        /// <see cref="RecurringCycleInfo"/>
        /// </returns>
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
        /// Gets customer of shopping cart.
        /// </summary>
        /// <returns>
        /// <see cref="Customer"/> of <see cref="OrganizedShoppingCartItem"/> or <c>null</c> if cart is empty.
        /// </returns>
        public static Customer GetCustomer(this IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Count > 0 ? cart[0].Item.Customer : null;
        }

        /// <summary>
        /// Returns filtered list of <see cref="ShoppingCartItem"/>s by <see cref="ShoppingCartType"/> and <paramref name="storeId"/>.
        /// </summary>
        /// <param name="cart">The cart collection the filter gets applied on.</param>
        /// <param name="cartType"><see cref="ShoppingCartType"/> to filter by.</param>
        /// <param name="storeId">Store identifier to filter by.</param>
        /// <returns><see cref="List{T}"/> of <see cref="ShoppingCartItem"/>.</returns>
        public static IList<ShoppingCartItem> FilterByCartType(this ICollection<ShoppingCartItem> cart, ShoppingCartType cartType, int? storeId = null)
        {
            Guard.NotNull(cart, nameof(cart));

            var filteredCartItems = cart.Where(x => x.ShoppingCartTypeId == (int)cartType);

            if (storeId.GetValueOrDefault() > 0)
            {
                filteredCartItems = filteredCartItems.Where(x => x.StoreId == storeId.Value);
            }

            return filteredCartItems.ToList();
        }
    }
}