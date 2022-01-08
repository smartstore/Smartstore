using Smartstore.Core.Catalog.Attributes;
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
        /// Deletes a shopping cart item  (including child items like bundle items).
        /// </summary>
        /// <param name="cartItem">Shopping cart item to delete.</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset checkout data.</param>
        /// <param name="removeInvalidCheckoutAttributes">
        /// A value indicating whether to remove invalid checkout attributes.
        /// For example removes checkout attributes that require shipping, if the cart does not require shipping at all.
        /// </param>
        Task DeleteCartItemAsync(ShoppingCartItem cartItem, bool resetCheckoutData = true, bool removeInvalidCheckoutAttributes = false);

        /// <summary>
        /// Deletes all shopping cart items in a cart (including child items like bundle items).
        /// </summary>
        /// <param name="cart">Shopping cart to delete.</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset checkout data.</param>
        /// <param name="removeInvalidCheckoutAttributes">
        /// A value indicating whether to remove invalid checkout attributes.
        /// For example removes checkout attributes that require shipping, if the cart does not require shipping at all.
        /// </param>
        /// <returns>Number of deleted shopping cart items.</returns>
        Task<int> DeleteCartAsync(ShoppingCart cart, bool resetCheckoutData = true, bool removeInvalidCheckoutAttributes = false);

        /// <summary>
        /// Gets a shopping cart for a customer.
        /// </summary>
        /// <param name="customer">Customer of cart. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="cartType">Shopping cart type.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Shopping cart.</returns>
        Task<ShoppingCart> GetCartAsync(Customer customer = null, ShoppingCartType cartType = ShoppingCartType.ShoppingCart, int storeId = 0);

        /// <summary>
        /// Migrates all cart items from one to another customer async.
        /// </summary>
        /// <param name="fromCustomer">From this customers cart.</param>
        /// <param name="toCustomer">To this customers cart.</param>       
        /// <returns><c>True</c> when cart has been successfully migrated, otherwise <c>false</c>.</returns>
        Task<bool> MigrateCartAsync(Customer fromCustomer, Customer toCustomer);

        /// <summary>
        /// Updates the shopping cart item by item identifier of customer.
        /// </summary>
        /// <param name="customer">Customer of cart items.</param>
        /// <param name="cartItemId">Cart item to update.</param>
        /// <param name="newQuantity">New quantitiy.</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset customer's checkout data.</param>
        /// <returns>List of error messages.</returns>
        Task<IList<string>> UpdateCartItemAsync(Customer customer, int cartItemId, int newQuantity, bool resetCheckoutData);

        /// <summary>
        /// Saves data entered on the shopping cart page (checkout attributes and whether to use reward points).
        /// Typically called right before entering checkout.
        /// It is used for example by payment buttons that skip checkout and redirect on a payment provider page.
        /// </summary>
        /// <param name="cart">Shopping cart, if <c>null</c> the cart of the current customer and store is loaded.</param>
        /// <param name="warnings">List of returned warnings.</param>
        /// <param name="query"><see cref="ProductVariantQuery"/> with checkout attributes to save.</param>
        /// <param name="useRewardPoints">A value indicating whether to use reward points during checkout. <c>null</c> to ignore.</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset customer's checkout data.</param>
        /// <param name="validateCheckoutAttributes">A value indicating whether to validate checkout attributes.</param>
        /// <returns><c>True</c> when the shopping cart is valid, otherwise <c>false</c>.</returns>
        Task<bool> SaveCartDataAsync(
            ShoppingCart cart,
            IList<string> warnings,
            ProductVariantQuery query,
            bool? useRewardPoints = null,
            bool resetCheckoutData = true,
            bool validateCheckoutAttributes = true);
    }
}