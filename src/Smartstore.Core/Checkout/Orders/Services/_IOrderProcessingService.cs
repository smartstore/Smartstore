using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Order processing interface.
    /// </summary>
    public partial interface IOrderProcessingService
    {
        /// <summary>
        /// Gets a value indicating whether an order can be refunded. Also checks if the payment provider supports to refund the payment.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be refunded.</returns>
        Task<bool> CanRefundAsync(Order order);

        /// <summary>
        /// Refunds an order. The payment is refunded at the payment provider if he supports this.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>List of errors if any.</returns>
        Task<IList<string>> RefundAsync(Order order);

        /// <summary>
        /// Gets a value indicating whether an order can be refunded "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be refunded.</returns>
        bool CanRefundOffline(Order order);

        /// <summary>
        /// Refunds an order "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        Task RefundOfflineAsync(Order order);

        /// <summary>
        /// Gets a value indicating whether an order can be partially refunded. Also checks if the payment provider supports to partially refund the payment.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        /// <returns>A value indicating whether an order can be partially refunded.</returns>
        Task<bool> CanPartiallyRefundAsync(Order order, Money amountToRefund);

        /// <summary>
        /// Partially refunds an order. The payment is partially refunded at the payment provider if he supports this.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        /// <returns>List of errors if any.</returns>
        Task<IList<string>> PartiallyRefundAsync(Order order, Money amountToRefund);

        /// <summary>
        /// Gets a value indicating whether an order can be partially refunded "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        /// <returns>A value indicating whether an order can be partially refunded "offline".</returns>
        bool CanPartiallyRefundOffline(Order order, Money amountToRefund);

        /// <summary>
        /// Partially refunds an order "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        Task PartiallyRefundOfflineAsync(Order order, Money amountToRefund);

        /// <summary>
        /// Gets a value indicating whether an order can be voided. Also checks if the payment provider supports to void the payment.
        /// </summary>
        /// <returns>A value indicating whether an order can be voided.</returns>
        Task<bool> CanVoidAsync(Order order);

        /// <summary>
        /// Voids an order. The payment is voided at the payment provider if he supports this.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>List of errors if any.</returns>
        Task<IList<string>> VoidAsync(Order order);

        /// <summary>
        /// Gets a value indicating whether an order can be voided "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be voided "offline".</returns>
        bool CanVoidOffline(Order order);

        /// <summary>
        /// Voids an order "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        Task VoidOfflineAsync(Order order);

        /// <summary>
        /// Place order items in shopping cart of the user who placed the order.
        /// </summary>
        /// <param name="order">Order.</param>
        Task ReOrderAsync(Order order);

        /// <summary>
        /// Check whether a return request is allowed.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether a return request is allowed.</returns>
        bool IsReturnRequestAllowed(Order order);

        /// <summary>
        /// Valdiate minimum order amount.
        /// Gets min order amount from customer role.
        /// When no minimum order amount is defined in customer role, default order settings are used as fallback if present.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="customerRoleIds">Customer role identifiers.</param>
        /// <returns><c>true</c>: OK. <c>false</c>: minimum order amount not reached.</returns>
        Task<(bool Valid, Money OrderTotalMinimum)> IsAboveOrderTotalMinimumAsync(IList<OrganizedShoppingCartItem> cart, int[] customerRoleIds);

        /// <summary>
        /// Valdiate maximum order amount.
        /// Gets max order amount from customer role.
        /// When no maximum order amount is defined in customer role, default order settings are used as fallback if present.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="customerRoleIds">Customer role identifiers.</param>
        /// <returns><c>true</c>: OK. <c>false</c>: maximum order amount exceeded.</returns>
        Task<(bool Valid, Money OrderTotalMaximum)> IsBelowOrderTotalMaximumAsync(IList<OrganizedShoppingCartItem> cart, int[] customerRoleIds);

        /// <summary>
        /// Adds a shipment to an order.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="trackingNumber">Tracking number.</param>
        /// <param name="trackingUrl">Tracking URL.</param>
        /// <param name="quantities">Quantities by order item identifiers. <c>null</c> to use the remaining total number of products for each order item.</param>
        /// <returns>New shipment, <c>null</c> if no shipment was added.</returns>
        Task<Shipment> AddShipmentAsync(Order order, string trackingNumber, string trackingUrl, Dictionary<int, int> quantities);
    }
}
