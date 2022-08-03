namespace Smartstore.Core.Checkout.Cart
{
    public static partial class IShoppingCartValidatorExtensions
    {
        /// <summary>
        /// Validates selected product attributes.
        /// </summary>
        /// <param name="cartItem">The cart item with product and attribute selection.</param>
        /// <param name="cartItems">The entire shopping cart.</param>
        /// <param name="warnings">List of returned warnings.</param>
        /// <returns><c>True</c> if selected attributes are valid, otherwise <c>false</c>.</returns>
        public static Task<bool> ValidateProductAttributesAsync(
            this IShoppingCartValidator validator,
            ShoppingCartItem cartItem,
            IEnumerable<OrganizedShoppingCartItem> cartItems,
            IList<string> warnings)
        {
            Guard.NotNull(validator, nameof(validator));
            Guard.NotNull(cartItem, nameof(cartItem));
            Guard.NotNull(warnings, nameof(warnings));

            return validator.ValidateProductAttributesAsync(
                cartItem.Product,
                cartItem.AttributeSelection,
                cartItem.StoreId,
                warnings,
                cartItem.Quantity,
                cartItem.Customer,
                cartItem.BundleItem,
                cartItem.ShoppingCartType,
                cartItems);
        }
    }
}
