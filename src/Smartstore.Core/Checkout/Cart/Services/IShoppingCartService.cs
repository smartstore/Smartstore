using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart service
    /// </summary>
    // TODO: (ms) (core) Revise dev docu (wip)
    public partial interface IShoppingCartService
    {
        /// <summary>
        /// Gets the shopping cart items count
        /// </summary>
        /// <param name="customer">Customer. Cannot be null.</param>
        /// <param name="cartType">Type of cart to get items count for</param>
        /// <param name="storeId">Store id</param>
        /// <returns>Sum of all item quantities</returns>
        Task<int> CountCartItemsAsync(Customer customer, ShoppingCartType cartType, int storeId = 0);

        /// <summary>
        /// Gets the shopping cart items
        /// </summary>
        /// <param name="customer">Customer. Cannot be null.</param>
        /// <param name="cartType">Type of cart to get items for</param>
        /// <param name="storeId">Store id</param>
        /// <returns>All cart items</returns>
        Task<List<OrganizedShoppingCartItem>> GetCartItemsAsync(Customer customer, ShoppingCartType cartType, int storeId = 0);

        ///// <summary>
        ///// Delete shopping cart item.
        ///// </summary>
        ///// <param name="shoppingCartItem">Shopping cart item</param>
        ///// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
        ///// <param name="ensureOnlyActiveCheckoutAttributes">A value indicating whether to ensure that only active checkout attributes are attached to the current customer</param>
        ///// <param name="deleteChildCartItems">A value indicating whether to delete child cart items</param>
        Task DeleteCartItemAsync(ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true, bool removeInvalidCheckoutAttributes = false, bool deleteChildCartItems = true);

        ///// <summary>
        ///// Deletes expired shopping cart items
        ///// </summary>
        ///// <param name="olderThanUtc">Older than date and time</param>
        ///// <param name="customerId"><c>null</c> to delete ALL cart items, or a customer id to only delete items of a single customer.</param>
        ///// <returns>Number of deleted items</returns>
        Task<int> DeleteExpiredCartItemsAsync(DateTime olderThanUtc, int? customerId = null);

        ///// <summary>
        ///// Finds a shopping cart item in the cart
        ///// </summary>
        ///// <param name="shoppingCart">Shopping cart</param>
        ///// <param name="shoppingCartType">Shopping cart type</param>
        ///// <param name="product">Product</param>
        ///// <param name="selectedAttributes">Selected attributes</param>
        ///// <param name="customerEnteredPrice">Price entered by a customer</param>
        ///// <returns>Found shopping cart item</returns>
        //OrganizedShoppingCartItem FindShoppingCartItemInTheCart(
        //          IList<OrganizedShoppingCartItem> shoppingCart,
        //          ShoppingCartType shoppingCartType,
        //          Product product,
        //          string selectedAttributes = "",
        //          decimal customerEnteredPrice = decimal.Zero);

        ///// <summary>
        ///// Add product to cart
        ///// </summary>
        ///// <param name="customer">The customer</param>
        ///// <param name="product">The product</param>
        ///// <param name="cartType">Cart type</param>
        ///// <param name="storeId">Store identifier</param>
        ///// <param name="selectedAttributes">Selected attributes</param>
        ///// <param name="customerEnteredPrice">Price entered by customer</param>
        ///// <param name="quantity">Quantity</param>
        ///// <param name="automaticallyAddRequiredProductsIfEnabled">Whether to add required products</param>
        ///// <param name="ctx">Add to cart context</param>
        ///// <returns>List with warnings</returns>
        Task<List<string>> AddToCartAsync(
            Customer customer,
            Product product,
            ShoppingCartType cartType,
            int storeId,
            ProductVariantAttributeSelection selection,
            decimal customerEnteredPrice,
            int quantity,
            bool automaticallyAddRequiredProductsIfEnabled,
            AddToCartContext ctx = null);

        ///// <summary>
        ///// Add product to cart
        ///// </summary>
        ///// <param name="ctx">Add to cart context</param>
        //Task AddToCart(AddToCartContext ctx);

        ///// <summary>
        ///// Stores the shopping card items in the database
        ///// </summary>
        ///// <param name="ctx">Add to cart context</param>
        Task AddToCartStoring(AddToCartContext ctx);

        ///// <summary>
        ///// Validates if all required attributes are selected
        ///// </summary>
        ///// <param name="selectedAttributes">Selected attributes</param>
        ///// <param name="product">Product</param>
        ///// <returns>bool</returns>
        //bool AreAllAttributesForCombinationSelected(string selectedAttributes, Product product);

        ///// <summary>
        ///// Updates the shopping cart item
        ///// </summary>
        ///// <param name="customer">Customer</param>
        ///// <param name="shoppingCartItemId">Shopping cart item identifier</param>
        ///// <param name="newQuantity">New shopping cart item quantity</param>
        ///// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
        ///// <returns>Warnings</returns>
        Task<IList<string>> UpdateShoppingCartItemAsync(Customer customer, int shoppingCartItemId, int newQuantity, bool resetCheckoutData);

        ///// <summary>
        ///// Migrate shopping cart
        ///// </summary>
        ///// <param name="fromCustomer">From customer</param>
        ///// <param name="toCustomer">To customer</param>
        Task MigrateShoppingCartAsync(Customer fromCustomer, Customer toCustomer);

        ///// <summary>
        ///// Copies a shopping cart item.
        ///// </summary>
        ///// <param name="sci">Shopping cart item</param>
        ///// <param name="customer">The customer</param>
        ///// <param name="cartType">Shopping cart type</param>
        ///// <param name="storeId">Store Id</param>
        ///// <param name="addRequiredProductsIfEnabled">Add required products if enabled</param>
        ///// <returns>List with add-to-cart warnings.</returns>
        Task<IList<string>> CopyAsync(OrganizedShoppingCartItem cartItem, Customer customer, ShoppingCartType cartType, int storeId, bool automaticallyAddRequiredProductsIfEnabled);

        ///// <summary>
        ///// Gets the subtotal of cart items for the current user
        ///// </summary>
        ///// <returns>unformatted subtotal of cart items for the current user</returns>
        //decimal GetCurrentCartSubTotal();

        ///// <summary>
        ///// Gets the subtotal of cart items for the current user
        ///// </summary>
        ///// <returns>unformatted subtotal of cart items for the current user</returns>
        //decimal GetCurrentCartSubTotal(IList<OrganizedShoppingCartItem> cart);

        ///// <summary>
        ///// Gets the formatted subtotal of cart items for the current user
        ///// </summary>
        ///// <returns>Formatted subtotal of cart items for the current user</returns>
        //string GetFormattedCurrentCartSubTotal();

        ///// <summary>
        ///// Gets the formatted subtotal of cart items for the current user
        ///// </summary>
        ///// <returns>Formatted subtotal of cart items for the current user</returns>
        //string GetFormattedCurrentCartSubTotal(IList<OrganizedShoppingCartItem> cart);

        ///// <summary>
        ///// Get open carts subtotal
        ///// </summary>
        ///// <returns>subtotal</returns>
        Task<decimal> GetAllOpenCartSubTotalAsync();

        ///// <summary>
        ///// Get open wishlists subtotal
        ///// </summary>
        ///// <returns>subtotal</returns>
        Task<decimal> GetAllOpenWishlistSubTotalAsync();
    }
}