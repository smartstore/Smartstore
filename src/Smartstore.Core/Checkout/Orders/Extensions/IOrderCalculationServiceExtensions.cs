using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore
{
    public static partial class IOrderCalculationServiceExtensions
    {
        /// <summary>
        /// Gets the discount amount and applied discount for a shipping total.
        /// </summary>
        /// <param name="orderCalculationService">Order calculation service.</param>
        /// <param name="shippingTotal">Shipping total amount.</param>
        /// <param name="customer">Customer.</param>
        /// <returns>The discount amount and applied discount.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<(Money Amount, Discount AppliedDiscount)> GetShippingDiscountAsync(
            this IOrderCalculationService orderCalculationService,
            Money shippingTotal,
            Customer customer)
        {
            Guard.NotNull(orderCalculationService, nameof(orderCalculationService));

            return orderCalculationService.GetDiscountAmountAsync(shippingTotal, DiscountType.AssignedToShipping, customer);
        }

        /// <summary>
        /// Gets the discount amount and applied discount for an subtotal total.
        /// </summary>
        /// <param name="orderCalculationService">Order calculation service.</param>
        /// <param name="orderSubTotal">Order subtotal.</param>
        /// <param name="customer">Customer.</param>
        /// <returns>The discount amount and applied discount.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<(Money Amount, Discount AppliedDiscount)> GetOrderSubtotalDiscountAsync(
            this IOrderCalculationService orderCalculationService,
            Money orderSubTotal,
            Customer customer)
        {
            Guard.NotNull(orderCalculationService, nameof(orderCalculationService));

            return orderCalculationService.GetDiscountAmountAsync(orderSubTotal, DiscountType.AssignedToOrderSubTotal, customer);
        }

        /// <summary>
        /// Gets the discount amount and applied discount for an order total.
        /// </summary>
        /// <param name="orderCalculationService">Order calculation service.</param>
        /// <param name="orderTotal">Order total.</param>
        /// <param name="customer">Customer.</param>
        /// <returns>The discount amount and applied discount.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<(Money Amount, Discount AppliedDiscount)> GetOrderTotalDiscountAsync(
            this IOrderCalculationService orderCalculationService,
            Money orderTotal,
            Customer customer)
        {
            Guard.NotNull(orderCalculationService, nameof(orderCalculationService));

            return orderCalculationService.GetDiscountAmountAsync(orderTotal, DiscountType.AssignedToOrderTotal, customer);
        }
    }
}
