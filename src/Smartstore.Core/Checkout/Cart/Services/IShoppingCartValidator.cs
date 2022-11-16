using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart validator interface.
    /// </summary>
    public interface IShoppingCartValidator
    {
        /// <summary>
        /// Validates customer's access to the shopping cart or the wish list.
        /// </summary>
        /// <param name="cartType">Shopping cart type which is to be validated.</param>
        /// <param name="customer">Customer whose permissions are to be validated.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <returns><c>True</c> when the customer is allowed to access respective cart type, otherwise <c>false</c>.</returns>
        Task<bool> ValidateAccessPermissionsAsync(Customer customer, ShoppingCartType cartType, IList<string> warnings);

        /// <summary>
        /// Validates bundle items.
        /// </summary>
        /// <param name="bundleItems"><see cref="ProductBundleItem"/> collection to validate.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <returns><c>True</c> when all bundle items are valid, otherwise <c>false</c>.</returns>
        bool ValidateBundleItem(ProductBundleItem bundleItems, IList<string> warnings);

        /// <summary>
        /// Validates shopping cart for products not found (null), recurring and standard product mix-ups as well as checkout attributes.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="warnings">List of returned warnings.</param>
        /// <param name="validateCheckoutAttributes">A value indicating whether to validate checkout attributes of <see cref="ShoppingCart.Customer"/>.</param>
        /// <returns><c>True</c> when all items as well as the <see cref="CheckoutAttributeSelection"/> are valid, otherwise <c>false</c>.</returns>
        Task<bool> ValidateCartAsync(ShoppingCart cart, IList<string> warnings, bool validateCheckoutAttributes = false);

        /// <summary>
        /// Validates add to cart item for product errors, attribute selection errors, gift card info errors, missing required products, bundle item and child items errors.
        /// </summary>
        /// <param name="ctx">Context object containing all the information about the item and context for adding it to the shopping cart.</param>
        /// <param name="cartItems">Shopping cart items of customer to validate.</param>
        /// <returns><c>True</c> when no error occured, otherwise <c>false</c>.</returns>
        Task<bool> ValidateAddToCartItemAsync(AddToCartContext ctx, ShoppingCartItem cartItem, IEnumerable<OrganizedShoppingCartItem> cartItems);

        /// <summary>
        /// Validates shopping cart for maximum number of cart/wish list items.
        /// </summary>
        /// <param name="cartType"><see cref="ShoppingCartType"/> of shopping cart to validate.</param>
        /// <param name="cartItemsCount">Number of shopping cart items.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <returns><c>True</c> when respective shopping cart limit is not exceeded, otherwise <c>false</c>.</returns>
        bool ValidateItemsMaximumCartQuantity(ShoppingCartType cartType, int cartItemsCount, IList<string> warnings);

        /// <summary>
        /// Validates cart item for <see cref="GiftCards.GiftCardInfo"/>.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="selection">Selected product attributes.</param>
        /// <param name="warnings">List of warnings.</param>
        bool ValidateGiftCardInfo(Product product, ProductVariantAttributeSelection selection, IList<string> warnings);

        /// <summary>
        /// Validates product settings, authorization and availability.
        /// </summary>
        /// <param name="cartItem">Shopping cart item with product and settings.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <param name="quantity">Quantity to validate. If <c>null</c>, <see cref="ShoppingCartItem.Quantity"/> is instead.</param>
        /// <returns><c>True</c> when product is valid, otherwise <c>false</c>.</returns>
        Task<bool> ValidateProductAsync(ShoppingCartItem cartItem, IList<string> warnings, int? storeId = null, int? quantity = null);

        /// <summary>
        /// Validates selected product attributes.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="selection">Selected attributes.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <param name="warnings">List of returned warnings.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="bundleItem">Product bundle item (if any).</param>/// 
        /// <param name="cartType">Shopping cart type.</param>
        /// <param name="cartItems">Entire shopping cart (if any).</param>
        /// <returns><c>True</c> if selected attributes are valid, otherwise <c>false</c>.</returns>
        Task<bool> ValidateProductAttributesAsync(
            Product product,
            ProductVariantAttributeSelection selection,
            int storeId,
            IList<string> warnings,
            int quantity = 1,
            Customer customer = null,
            ProductBundleItem bundleItem = null,
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart,
            IEnumerable<OrganizedShoppingCartItem> cartItems = null);

        /// <summary>
        /// Validates the product on all required products.
        /// </summary>
        /// <param name="product">Product to validate.</param>
        /// <param name="cartItems">Shopping cart items used for required products validation.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <returns><c>True</c> when the shopping cart already contains all required products, otherwise <c>false</c>.</returns>
        Task<bool> ValidateRequiredProductsAsync(Product product, IEnumerable<OrganizedShoppingCartItem> cartItems, IList<string> warnings);
    }
}