using System.Collections.Generic;
using System.Threading.Tasks;
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
        bool ValidateBundleItems(IEnumerable<ProductBundleItem> bundleItems, IList<string> warnings);

        /// <summary>
        /// Validates shopping cart for products not found (null), recurring and standard product mix-ups as well as checkout attributes.
        /// </summary>
        /// <param name="cartItems">Shopping cart items of customer to validate.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <param name="validateCheckoutAttributes">A value indicating whether to validate the <see cref="CheckoutAttributeSelection"/>.</param>
        /// <param name="attributeSelection"><see cref="CheckoutAttributeSelection"/> of customer. Cannot be null when <paramref name="validateCheckoutAttributes"/> is <c>true</c>, otherwise there is no attributes selection check.</param>
        /// <returns><c>True</c> when all items as well as the <see cref="CheckoutAttributeSelection"/> are valid, otherwise <c>false</c>.</returns>
        Task<bool> ValidateCartAsync(IEnumerable<OrganizedShoppingCartItem> cartItems, IList<string> warnings, bool validateCheckoutAttributes = false, CheckoutAttributeSelection attributeSelection = null);

        /// <summary>
        /// Validates add to cart item for product errors, attribute selection errors, gift card info errors, missing required products, bundle item and child items errors.
        /// </summary>
        /// <param name="ctx">Context object containing all the information about the item and context for adding it to the shopping cart.</param>
        /// <param name="cartItems">Shopping cart items of customer to validate.</param>
        /// <returns><c>True</c> when no error occured, otherwise <c>false</c>.</returns>
        Task<bool> ValidateAddToCartItemAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> cartItems);

        /// <summary>
        /// Validates shopping cart for maximum number of cart/wish list items.
        /// </summary>
        /// <param name="cartType"><see cref="ShoppingCartType"/> of shopping cart to validate.</param>
        /// <param name="cartItemsCount">Number of shopping cart items.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <returns><c>True</c> when respective shopping cart limit is not exceeded, otherwise <c>false</c>.</returns>
        bool ValidateItemsMaximumCartQuantity(ShoppingCartType cartType, int cartItemsCount, IList<string> Warnings);

        /// <summary>
        /// Validates cart item for <see cref="GiftCards.GiftCardInfo"/>.
        /// </summary>
        /// <param name="cartItem">Shopping cart item with product and attribute selection informations.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <returns></returns>
        bool ValidateGiftCardInfo(ShoppingCartItem cartItem, IList<string> warnings);

        /// <summary>
        /// Validates product settings, authorization and availability.
        /// </summary>
        /// <param name="cartItem">Shopping cart item with product and settings.</param>
        /// <param name="warnings">List of errors as string.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns><c>True</c> when product is valid, otherwise <c>false</c>.</returns>
        Task<bool> ValidateProductAsync(ShoppingCartItem cartItem, IList<string> warnings, int? storeId = null);

        /// <summary>
        /// Validates product attribute selection and variant combinations.
        /// </summary>
        /// <param name="cartItem">The cart item with product and attribute selection informations.</param>
        /// <param name="cartItems">The entire shopping cart items.</param>
        /// <param name="warnings">List of errors as sting.</param>
        /// <returns><c>True</c> when products and their attribute selections are valid, otherwise <c>false</c>.</returns>
        Task<bool> ValidateProductAttributesAsync(ShoppingCartItem cartItem, IEnumerable<OrganizedShoppingCartItem> cartItems, IList<string> warnings);

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