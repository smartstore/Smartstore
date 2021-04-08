using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart service interface.
    /// </summary>
    public interface IShoppingCartService
    {
        /// <summary>
        /// Adds <see cref="AddToCartContext.Item"/> as well as all <see cref="AddToCartContext.ChildItems"/> to customers shopping cart 
        /// and automatically updates database async.
        /// </summary>
        /// <param name="ctx">Context info about the items to be added to the cart.</param>
        Task AddItemToCartAsync(AddToCartContext ctx);

        /// <summary>
        /// Adds or creates cart items from <see cref="AddToCartContext.Product"/> to the customers shopping cart and automatically. 
        /// Updates the database async as long as the item is not a bundle or it is a bundle and <see cref="AddToCartContext.AutomaticallyAddBundleProducts"/> is true.
        /// </summary>
        /// <remarks>
        /// If you manually assign <see cref="AddToCartContext"/> with bundle items, then use <see cref="AddItemToCartAsync(AddToCartContext)"/> 
        /// afterwards to commit items to the database.
        /// </remarks>
        /// <param name="ctx">Context info about the product to be added to the cart.</param>
        /// <returns><c>True</c> if the item was successfully added to the cart; otherwise <c>false</c></returns>
        Task<bool> AddToCartAsync(AddToCartContext ctx);

        /// <summary>
        /// Copies an item and all its child items to another cart async.
        /// </summary>
        /// <param name="ctx">Context information about the item to be copied and the client to be received.</param>
        /// <returns><c>True</c> if the item was successfully added to the cart; otherwise <c>false</c></returns>
        Task<bool> CopyAsync(AddToCartContext ctx);

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
        /// Gets the customers shopping cart items async.
        /// </summary>
        /// <param name="customer">Customer of cart. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="cartType">Shopping cart type.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>List of error messages.</returns>
        Task<List<OrganizedShoppingCartItem>> GetCartItemsAsync(
            Customer customer = null, 
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart, 
            int storeId = 0);

        /// <summary>
        /// Migrates all cart items from one to another customer async.
        /// </summary>
        /// <param name="fromCustomer">From this customers cart.</param>
        /// <param name="toCustomer">To this customers cart.</param>       
        /// <returns><c>True</c> if all the cart items were successfully added to the (other) cart; otherwise <c>false</c></returns>
        Task<bool> MigrateCartAsync(Customer fromCustomer, Customer toCustomer);

        /// <summary>
        /// Updates the shopping cart item by item identifier of customer.
        /// </summary>
        /// <param name="customer">Customer of cart items.</param>
        /// <param name="cartItemId">Cart item to update.</param>
        /// <param name="newQuantity">New quantitiy.</param>
        /// <param name="resetCheckoutData">Value indicating whether to reset checkout data.</param>
        /// <returns>List of error messages.</returns>
        Task<IList<string>> UpdateCartItemAsync(Customer customer, int cartItemId, int newQuantity, bool resetCheckoutData);
    }
}