using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart validator interface
    /// </summary>
    // TODO: (ms) (core) (wip) Revise dev docu
    // TODO: (ms) (core) All validation methods should return bool. Warning list instance should be passed to method either standalone or as part of a context class. TBD with MC.
    public interface IShoppingCartValidator
    {
        Task<IList<string>> ValidateAccessPermissionsAsync(AddToCartContext ctx);

        IList<string> ValidateBundleItems(IEnumerable<ProductBundleItem> bundleItems);

        Task<IList<string>> ValidateCartAsync(IEnumerable<OrganizedShoppingCartItem> cartItems, CheckoutAttributeSelection attributeSelection, bool validateCheckoutAttributes);

        Task<IList<string>> ValidateCartItemAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart);

        IList<string> ValidateCartItemsMaximum(ShoppingCartType cartType, int cartItemsCount);

        IList<string> ValidateGiftCardInfo(AddToCartContext ctx);

        Task<IList<string>> ValidateProductAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart);

        Task<IList<string>> ValidateProductAttributesAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart);

        Task<IList<string>> ValidateRequiredProductsAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> cartItems);
    }

    public static class IShoppingCartValidatorExtensions
    {
        /// <summary>
        /// Validates a single cart item for bundle items.
        /// </summary>
        /// <param name="cartValidator"></param>
        /// <param name="bundleItem"></param>
        /// <returns></returns>
        public static IList<string> ValidateBundleItem(this IShoppingCartValidator cartValidator, ProductBundleItem bundleItem)
        {
            Guard.NotNull(cartValidator, nameof(cartValidator));
            Guard.NotNull(bundleItem, nameof(bundleItem));

            return cartValidator.ValidateBundleItems(new[] { bundleItem });
        }
    }
}