using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Customers;

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
}