using System.Threading.Tasks;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
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
        public static async Task<(decimal Amount, Discount AppliedDiscount)> GetShippingDiscountAsync(
            this IOrderCalculationService orderCalculationService,
            decimal shippingTotal,
            Customer customer)
        {
            Guard.NotNull(orderCalculationService, nameof(orderCalculationService));

            return await orderCalculationService.GetDiscountAmountAsync(shippingTotal, DiscountType.AssignedToShipping, customer);
        }

        /// <summary>
        /// Gets the discount amount and applied discount for an subtotal total.
        /// </summary>
        /// <param name="orderCalculationService">Order calculation service.</param>
        /// <param name="orderSubTotal">Order subtotal.</param>
        /// <param name="customer">Customer.</param>
        /// <returns>The discount amount and applied discount.</returns>
        public static async Task<(decimal Amount, Discount AppliedDiscount)> GetOrderSubtotalDiscountAsync(
            this IOrderCalculationService orderCalculationService, 
            decimal orderSubTotal, 
            Customer customer)
        {
            Guard.NotNull(orderCalculationService, nameof(orderCalculationService));

            return await orderCalculationService.GetDiscountAmountAsync(orderSubTotal, DiscountType.AssignedToOrderSubTotal, customer, false);
        }

        /// <summary>
        /// Gets the discount amount and applied discount for an order total.
        /// </summary>
        /// <param name="orderCalculationService">Order calculation service.</param>
        /// <param name="orderTotal">Order total.</param>
        /// <param name="customer">Customer.</param>
        /// <returns>The discount amount and applied discount.</returns>
        public static async Task<(decimal Amount, Discount AppliedDiscount)> GetOrderTotalDiscountAsync(
            this IOrderCalculationService orderCalculationService,
            decimal orderTotal, 
            Customer customer)
        {
            Guard.NotNull(orderCalculationService, nameof(orderCalculationService));

            return await orderCalculationService.GetDiscountAmountAsync(orderTotal, DiscountType.AssignedToOrderTotal, customer);
        }
    }
}
