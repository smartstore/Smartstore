using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Order calculation service.
    /// </summary>
    public partial interface IOrderCalculationService
    {
        /// <summary>
        /// Gets the shopping cart total.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includeRewardPoints">A value indicating whether to include reward points.</param>
        /// <param name="includePaymentAdditionalFee">A value indicating whether to include payment additional fee of the selected payment method.</param>
        /// <param name="includeCreditBalance">A value indicating whether to include credit balance.</param>
        /// <returns></returns>
        Task<ShoppingCartTotal> GetShoppingCartTotalAsync(
            IList<OrganizedShoppingCartItem> cart,
            bool includeRewardPoints = true,
            bool includePaymentAdditionalFee = true,
            bool includeCreditBalance = true);

        /// <summary>
        /// Gets the shopping cart subtotal.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includeTax">A value indicating whether the calculated price should include tax.
        /// If <c>null</c>, will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.</param>
        /// <returns>Shopping cart subtotal.</returns>
        Task<ShoppingCartSubTotal> GetShoppingCartSubTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includeTax = null);

        /// <summary>
        /// Gets the shopping cart shipping total.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includeTax">A value indicating whether the calculated price should include tax.
        /// If <c>null</c>, will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.</param>
        /// <returns>Shopping cart shipping total.</returns>
        Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(IList<OrganizedShoppingCartItem> cart, bool? includeTax = null);

        /// <summary>
        /// Gets the shopping cart tax total.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includePaymentAdditionalFee">A value indicating whether to include payment additional fee of the selected payment method.</param>
        /// <returns>The tax total amount and applied tax rates.</returns>
        Task<(Money Amount, TaxRatesDictionary taxRates)> GetTaxTotalAsync(IList<OrganizedShoppingCartItem> cart, bool includePaymentAdditionalFee = true);

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
        Task<Money> GetShoppingCartAdditionalShippingChargeAsync(IList<OrganizedShoppingCartItem> cart);

        /// <summary>
        /// Gets the payment fee for a cart.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="fixedFeeOrPercentage">A fixed fee value or a percentage value.</param>
        /// <param name="usePercentage">A value indicating whether fixedFeeOrPercentage is a fixed or percentage value.</param>
        /// <returns>Additional payment method fee.</returns>
        Task<Money> GetShoppingCartPaymentFee(IList<OrganizedShoppingCartItem> cart, decimal fixedFeeOrPercentage, bool usePercentage);

        /// <summary>
        /// Adjusts the shipping rate (free shipping, additional charges, discounts).
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="shippingRate">Shipping rate.</param>
        /// <param name="shippingOption">Shipping option.</param>
        /// <param name="shippingMethods">Shipping methods.</param>
        /// <returns>Adjusted shipping rate.</returns>
        Task<(Money Amount, Discount AppliedDiscount)> AdjustShippingRateAsync(
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
        Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(Money money, DiscountType discountType, Customer customer, bool round = true);

        /// <summary>
        /// Converts reward points to a primary store currency amount.
        /// </summary>
        /// <param name="rewardPoints">Reward points.</param>
        /// <returns>Converted currency amount.</returns>
        Money ConvertRewardPointsToMoney(int rewardPoints);

        /// <summary>
        /// Converts a primary store currency amount to reward points.
        /// </summary>
        /// <param name="amount">Currency amount.</param>
        /// <returns>Converted points.</returns>
        int ConvertMoneyToRewardPoints(Money amount);
    }
}
