using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart service interface
    /// </summary>
    // TODO: (ms) (core) (wip) Revise dev docu of interface
    public interface IShoppingCartService
    {
        Task<IList<string>> AddToCartAsync(AddToCartContext ctx);

        Task<IList<string>> CopyAsync(AddToCartContext ctx);

        Task<int> CountCartItemsAsync(
            Customer customer = null, 
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart, 
            int storeId = 0);

        Task<int> DeleteCartItemsAsync(
            IEnumerable<ShoppingCartItem> cartItems, 
            bool resetCheckoutData = true, 
            bool removeInvalidCheckoutAttributes = false, 
            bool deleteChildCartItems = true);

        Task<int> DeleteExpiredCartItemsAsync(DateTime olderThanUtc, Customer customer);

        Task<IList<OrganizedShoppingCartItem>> GetCartItemsAsync(
            Customer customer = null, 
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart, 
            int storeId = 0);

        Task<decimal> GetOpenCartsSubTotalAsync();

        Task<decimal> GetOpenWishlistsSubTotalAsync();

        Task MigrateCartAsync(Customer fromCustomer, Customer toCustomer);

        Task<IList<string>> UpdateCartItemAsync(Customer customer, int cartItemId, int newQuantity, bool resetCheckoutData);
    }

    public static class IShoppingCartServiceExtensions
    {
        /// <summary>
        /// Removes a single cart item from shopping cart of <see cref="ShoppingCartItem.Customer"/>.
        /// </summary>        
        /// <param name="cartItem">Cart item to remove from shopping cart.</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset checkout data.</param>
        /// <param name="removeInvalidCheckoutAttributes">A value indicating whether to remove incalid checkout attributes.</param>
        /// <param name="deleteChildCartItems">A value indicating whether to delete child cart items of <c>cartItem.</c></param>
        /// <returns>Number of deleted entries.</returns>
        public static Task<int> DeleteCartItemAsync(this IShoppingCartService cartService,
            ShoppingCartItem cartItem,
            bool resetCheckoutData = true,
            bool removeInvalidCheckoutAttributes = false,
            bool deleteChildCartItems = true)
        {
            Guard.NotNull(cartService, nameof(cartService));
            Guard.NotNull(cartItem, nameof(cartItem));

            return cartService.DeleteCartItemsAsync(
                new[] { cartItem },
                resetCheckoutData,
                removeInvalidCheckoutAttributes,
                deleteChildCartItems);
        }
    }
}