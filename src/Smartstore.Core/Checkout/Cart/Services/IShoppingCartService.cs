using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart service interface.
    /// </summary>
    public interface IShoppingCartService
    {
        /// <summary>
        /// Adds an item to cart and updates database async.
        /// </summary>
        /// <param name="ctx">Context info about the product to added to the cart.</param>
        /// <returns>List of error messages.</returns>
        Task<IList<string>> AddToCartAsync(AddToCartContext ctx);

        /// <summary>
        /// Copies an item and all its child items to another cart async.
        /// </summary>
        /// <param name="ctx">Context information about the item to be copied and the client to be received.</param>
        /// <returns>List of error messages.</returns>
        Task<IList<string>> CopyAsync(AddToCartContext ctx);

        /// <summary>
        /// Gets the customers shopping cart items count async.
        /// </summary>
        /// <param name="customer">Customer of cart to be counted.</param>
        /// <param name="cartType">Shopping cart type.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Number of items.</returns>
        Task<int> CountCartItemsAsync(
            Customer customer = null, 
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart, 
            int storeId = 0);

        /// <summary>
        /// Deletes shopping cart items async.
        /// </summary>
        /// <param name="cartItems">List of cart items to be removed.</param>
        /// <param name="resetCheckoutData">Value indicating whether to reset checkout data.</param>
        /// <param name="removeInvalidCheckoutAttributes">Value indicating whether to remove checkout attributes that become invalid.</param>
        /// <param name="deleteChildCartItems">Value indicating whether to delete child items.</param>
        /// <returns>Number of items deleted.</returns>
        Task<int> DeleteCartItemsAsync(
            IEnumerable<ShoppingCartItem> cartItems, 
            bool resetCheckoutData = true, 
            bool removeInvalidCheckoutAttributes = false, 
            bool deleteChildCartItems = true);

        /// <summary>
        /// Deletes expired shopping cart items async.
        /// </summary>
        /// <param name="olderThanUtc">Expiry threshold date time</param>
        /// <param name="customer">Customer of cart.</param>
        /// <returns>Number of items deleted.</returns>
        Task<int> DeleteExpiredCartItemsAsync(DateTime olderThanUtc, Customer customer);

        /// <summary>
        /// Gets the customers shopping cart items async.
        /// </summary>
        /// <param name="customer">Customer of cart.</param>
        /// <param name="cartType">Shopping cart type.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>List of error messages.</returns>
        Task<IList<OrganizedShoppingCartItem>> GetCartItemsAsync(
            Customer customer = null, 
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart, 
            int storeId = 0);

        /// <summary>
        /// Migrates all cart items from one to another customer async.
        /// </summary>
        /// <param name="fromCustomer">From this customer.</param>
        /// <param name="toCustomer">To this customer.</param>        
        Task MigrateCartAsync(Customer fromCustomer, Customer toCustomer);

        /// <summary>
        /// Updates the shopping cart item by item identifier of customer.
        /// </summary>
        /// <param name="customer">Customer of cart items.</param>
        /// <param name="cartItemId">Cart item to update.</param>
        /// <param name="newQuantity">New quantitiy.</param>
        /// <param name="resetCheckoutData">Value indicating whether to reset checkout data.</param>
        /// <returns>List of error messages.</returns>
        Task<IList<string>> UpdateCartItemAsync(Customer customer, int cartItemId, int newQuantity, bool resetCheckoutData);

        /// <summary>
        /// Gets all open carts sub totals.
        /// </summary>
        /// <param name="cartType">Shopping cart type.</param>
        /// <returns>Sub total of all open carts.</returns>
        Task<Money> GetOpenCartsSubTotalAsync(ShoppingCartType cartType);

        /// <summary>
        /// Gets the cart subtotal converted into <see cref="IWorkContext.WorkingCurrency"/> for the current user.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <returns>Converted cart subtotal.</returns>
        Task<Money> GetCurrentCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart = null);
    }
}