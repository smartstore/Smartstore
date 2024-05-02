using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Order calculation service.
    /// OrderCalculationService internally calculates in the primary currency, 
    /// consequently currency values are also returned in the primary currency by default.
    /// </summary>
    public partial interface IOrderCalculationService
    {
        /// <summary>
        /// Gets the shopping cart total.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includeRewardPoints">A value indicating whether to include reward points.</param>
        /// <param name="includePaymentFee">A value indicating whether to include payment additional fee of the selected payment method.</param>
        /// <param name="includeCreditBalance">A value indicating whether to include credit balance.</param>
        /// <param name="batchContext">The product batch context used to load all cart products in one go. Will be created internally if <c>null</c>.</param>
        /// <param name="cache">A value indicating whether to cache the result.</param>
        /// <returns>Shopping cart total.</returns>
        Task<ShoppingCartTotal> GetShoppingCartTotalAsync(
            ShoppingCart cart,
            bool includeRewardPoints = true,
            bool includePaymentFee = true,
            bool includeCreditBalance = true,
            ProductBatchContext batchContext = null,
            bool cache = true);

        /// <summary>
        /// Gets the shopping cart subtotal.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includeTax">A value indicating whether the calculated price should include tax.
        /// If <c>null</c>, will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.</param>
        /// <param name="batchContext">The product batch context used to load all cart products in one go. Will be created internally if <c>null</c>.</param>
        /// <param name="activeOnly">
        /// A value indicating whether to only include active cart items in calculation.
        /// <c>false</c> to include all cart items (default). <c>true</c> to ignore deactivated items.
        /// </param>
        /// <param name="cache">A value indicating whether to cache the result.</param>
        /// <returns>Shopping cart subtotal.</returns>
        Task<ShoppingCartSubtotal> GetShoppingCartSubtotalAsync(
            ShoppingCart cart,
            bool? includeTax = null,
            ProductBatchContext batchContext = null,
            bool activeOnly = false,
            bool cache = true);

        /// <summary>
        /// Gets the shopping cart shipping total.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includeTax">A value indicating whether the calculated price should include tax.
        /// If <c>null</c>, will be obtained via <see cref="IWorkContext.TaxDisplayType"/>.</param>
        /// <returns>Shopping cart shipping total.</returns>
        Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotalAsync(ShoppingCart cart, bool? includeTax = null);

        /// <summary>
        /// Gets the shopping cart tax total in the primary currency.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="includePaymentFee">A value indicating whether to include payment additional fee of the selected payment method.</param>
        /// <returns>The tax total amount in the primary currency and applied tax rates.</returns>
        Task<(Money Price, TaxRatesDictionary TaxRates)> GetShoppingCartTaxTotalAsync(ShoppingCart cart, bool includePaymentFee = true);

        /// <summary>
        /// Gets a value indicating whether shipping is free.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <returns>A value indicating whether shipping is free.</returns>
        Task<bool> IsFreeShippingAsync(ShoppingCart cart);

        /// <summary>
        /// Gets the cart's additional shipping charge in the primary currency.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <returns>Additional shipping charge in the primary currency.</returns>
        Task<Money> GetShoppingCartShippingChargeAsync(ShoppingCart cart);

        /// <summary>
        /// Gets the cart's payment fee for in the primary currency.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>Additional payment method fee in the primary currency.</returns>
        Task<Money> GetShoppingCartPaymentFeeAsync(ShoppingCart cart, string paymentMethodSystemName);

        /// <summary>
        /// Adjusts the shipping rate (free shipping, additional charges, discounts).
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="shippingRate">Shipping rate.</param>
        /// <param name="shippingOption">Shipping option.</param>
        /// <param name="shippingMethods">Shipping methods.</param>
        /// <returns>Adjusted shipping rate in the primary currency.</returns>
        Task<(decimal Amount, Discount AppliedDiscount)> AdjustShippingRateAsync(
            ShoppingCart cart,
            decimal shippingRate,
            ShippingOption shippingOption,
            IList<ShippingMethod> shippingMethods);

        /// <summary>
        /// Applies a <paramref name="couponCode"/> to <see cref="ShoppingCart.Customer"/>.
        /// The caller is responsible for database commit.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="couponCode">The discount coupon code to apply.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="couponCode"/> was applied to <see cref="ShoppingCart.Customer"/>, otherwise <c>false</c>.
        /// Also returns the discount to which <paramref name="couponCode"/> belongs.
        /// <c>null</c> if either the discount or the <paramref name="couponCode"/> is invalid.
        /// </returns>
        Task<(bool Applied, Discount AppliedDiscount)> ApplyDiscountCouponAsync(ShoppingCart cart, string couponCode);

        /// <summary>
        /// Gets the discount amount in the primary currency and applied discount for a given amount.
        /// </summary>
        /// <param name="amount">Amount.</param>
        /// <param name="discountType">Discount type.</param>
        /// <param name="customer">Customer</param>
        /// <returns>The discount amount in the primary currency and applied discount.</returns>
        Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(Money amount, DiscountType discountType, Customer customer);

        /// <summary>
        /// Converts reward points to an amount in the primary currency.
        /// </summary>
        /// <param name="rewardPoints">Reward points.</param>
        /// <returns>Converted amount in the primary currency.</returns>
        Money ConvertRewardPointsToAmount(int rewardPoints);

        /// <summary>
        /// Converts a primary currency amount to reward points.
        /// </summary>
        /// <param name="amount">Currency amount.</param>
        /// <returns>Converted points.</returns>
        int ConvertAmountToRewardPoints(Money amount);
    }
}
