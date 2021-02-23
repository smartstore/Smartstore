using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Order calculation service.
    /// </summary>
    public partial interface IOrderCalculationService
    {
        /// <summary>
        /// Gets the shopping cart subtotal.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includingTax">A value indicating whether the calculated price should include tax.
        /// If <c>null</c>, will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.</param>
        /// <returns>Shopping cart subtotal.</returns>
        Task<ShoppingCartSubTotal> GetShoppingCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includingTax = null);

        /// <summary>
        /// Gets the shopping cart shipping total.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includingTax">A value indicating whether the calculated price should include tax.
        /// If <c>null</c>, will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.</param>
        /// <returns>Shopping cart shipping total.</returns>
        Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includingTax = null);

        /// <summary>
        /// Gets a value indicating whether shipping is free.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <returns>A value indicating whether shipping is free.</returns>
        Task<bool> IsFreeShippingAsync(IList<OrganizedShoppingCartItem> cart);

        /// <summary>
        /// Gets the additional shipping charge for a cart.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <returns>Additional shipping charge.</returns>
        Task<decimal> GetShoppingCartAdditionalShippingChargeAsync(IList<OrganizedShoppingCartItem> cart);

        /// <summary>
        /// Adjusts the shipping rate (free shipping, additional charges, discounts).
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="shippingRate">Shipping rate.</param>
        /// <param name="shippingOption">Shipping option.</param>
        /// <param name="shippingMethods">Shipping methods.</param>
        /// <returns>Adjusted shipping rate.</returns>
        Task<(decimal Amount, Discount AppliedDiscount)> AdjustShippingRateAsync(
            IList<OrganizedShoppingCartItem> cart,
            decimal shippingRate,
            ShippingOption shippingOption,
            IList<ShippingMethod> shippingMethods);

        /// <summary>
        /// Gets the discount amount and applied discount for a given amount.
        /// </summary>
        /// <param name="amount">Amount.</param>
        /// <param name="discountType">Discount type.</param>
        /// <param name="customer">Customer</param>
        /// <param name="round">A value indicating whether to round the discount amount.</param>
        /// <returns>The discount amount and applied discount.</returns>
        Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(decimal amount, DiscountType discountType, Customer customer, bool round = true);

        /// <summary>
        /// Converts reward points to a primary store currency amount.
        /// </summary>
        /// <param name="rewardPoints">Reward points.</param>
        /// <returns>Converted currency amount.</returns>
        decimal ConvertRewardPointsToAmount(int rewardPoints);

        /// <summary>
        /// Converts a primary store currency amount to reward points.
        /// </summary>
        /// <param name="amount">Currency amount.</param>
        /// <returns>Converted points.</returns>
        int ConvertAmountToRewardPoints(decimal amount);
    }
}
