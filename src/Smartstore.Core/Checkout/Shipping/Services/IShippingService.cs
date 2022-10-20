using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Shipping service interface.
    /// </summary>
    public partial interface IShippingService
    {
        /// <summary>
        /// Gets active shipping rate computation methods.
        /// </summary>
        /// <param name="systemName">Filters methods by provider system name. <c>null</c> to load all methods.</param>
		/// <param name="storeId">Filters methods by store identifier. 0 to load all methods.</param>
        /// <remarks>
        /// Tries to get any fallback computation method when no active method was found.
        /// Throws an exception when no computation method was found at all.
        /// </remarks>
        /// <returns>Shipping rate computation methods.</returns>
        IEnumerable<Provider<IShippingRateComputationMethod>> LoadActiveShippingRateComputationMethods(int storeId = 0, string systemName = null);

        /// <summary>
        /// Gets all shipping methods.
        /// </summary>
        /// <param name="storeId">Filters methods by store identifier. 0 to load all methods.</param>
        /// <param name="matchRules">A value indicating whether shipping methods must match cart rules.</param>
        /// <returns>Shipping methods.</returns>
        Task<List<ShippingMethod>> GetAllShippingMethodsAsync(int storeId = 0, bool matchRules = false);

        /// <summary>
        /// Gets shopping cart items (total) weight.
        /// </summary>
        /// <param name="cartItem">Shopping cart.</param>
        /// <param name="multiplyByQuantity">A value indicating whether the item weight is to be multiplied by the quantity (total item weight).</param>
        /// <remarks>Includes additional <see cref="ProductVariantAttribute"/> weight adjustments.</remarks>
        /// <returns>Shopping cart item weight.</returns>
        Task<decimal> GetCartItemWeightAsync(OrganizedShoppingCartItem cartItem, bool multiplyByQuantity = true);

        /// <summary>
        /// Gets shopping cart total weight.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
		/// <param name="includeFreeShippingProducts">A value indicating whether to include products with free shipping.</param>
        /// <remarks>Includes <see cref="CheckoutAttribute"/> of the customer in the calculations, if available.</remarks>
        /// <returns>Shopping cart total weight.</returns>
        Task<decimal> GetCartTotalWeightAsync(ShoppingCart cart, bool includeFreeShippingProducts = true);

        /// <summary>
        /// Creates a shipping option request.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="shippingAddress">Shipping address.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns><see cref="ShippingOptionRequest"/>.</returns>
        ShippingOptionRequest CreateShippingOptionRequest(ShoppingCart cart, Address shippingAddress, int storeId);

        /// <summary>
        /// Gets shipping options for a shipping option request.
        /// </summary>
        /// <param name="request">Shipping option request.</param>
        /// <param name="allowedShippingRateComputationMethodSystemName">
        /// Filter by shipping rate computation method system name.
        /// <c>null</c> to load shipping options of all shipping rate computation methods.
        /// </param>
        /// <remarks>
        /// Always returns <see cref="ShippingOption"/> if there are any, even when there are warnings.
        /// </remarks>
        /// <returns>Get shipping option resopnse</returns>
        Task<ShippingOptionResponse> GetShippingOptionsAsync(ShippingOptionRequest request, string allowedShippingRateComputationMethodSystemName = null);
    }
}