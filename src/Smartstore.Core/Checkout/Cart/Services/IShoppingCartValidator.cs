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
    public interface IShoppingCartValidator
    {
        Task<IList<string>> ValidateAccessPermissionsAsync(AddToCartContext ctx);

        IList<string> ValidateBundleItems(IList<ProductBundleItem> bundleItems);

        Task<IList<string>> ValidateCartCheckoutAsync(
            IEnumerable<OrganizedShoppingCartItem> cartItems,
            CheckoutAttributeSelection attributeSelection,
            bool validateCheckoutAttributes);

        Task<IList<string>> ValidateCartItemAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart);

        IList<string> ValidateCartItemsMaximum(ShoppingCartType cartType, int cartItemsCount);

        IList<string> ValidateGiftCardInfo(AddToCartContext ctx);

        Task<IList<string>> ValidateProductAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart);

        Task<IList<string>> ValidateProductAttributesAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> shoppingCart);

        Task<IList<string>> ValidateRequiredProductsAsync(AddToCartContext ctx, IEnumerable<OrganizedShoppingCartItem> cartItems);
    }
}