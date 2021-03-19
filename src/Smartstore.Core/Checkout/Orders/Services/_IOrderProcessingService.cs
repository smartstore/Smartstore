using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Order processing interface.
    /// </summary>
    public partial interface IOrderProcessingService
    {
        /// <summary>
        /// Cancels an order.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="notifyCustomer"><c>true</c> to notify customer.</param>
        Task CancelOrderAsync(Order order, bool notifyCustomer);

        /// <summary>
        /// Marks an order as completed.
        /// </summary>
        /// <param name="order">Order.</param>
        Task CompleteOrderAsync(Order order);

        /// <summary>
        /// Deletes an order.
        /// </summary>
        /// <param name="order">Order.</param>
        Task DeleteOrderAsync(Order order);

        /// <summary>
        /// Place order items in shopping cart of the user who placed the order.
        /// </summary>
        /// <param name="order">Order.</param>
        Task ReOrderAsync(Order order);

        /// <summary>
        /// Marks a shipment as shipped.
        /// </summary>
        /// <param name="shipment">Shipment.</param>
        /// <param name="notifyCustomer"><c>true</c> to notify customer.</param>
        Task ShipAsync(Shipment shipment, bool notifyCustomer);

        /// <summary>
        /// Marks a shipment as delivered.
        /// </summary>
        /// <param name="shipment">Shipment.</param>
        /// <param name="notifyCustomer"><c>true</c> to notify customer.</param>
        Task DeliverAsync(Shipment shipment, bool notifyCustomer);

        /// <summary>
        /// Check whether a return request is allowed.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether a return request is allowed.</returns>
        bool IsReturnRequestAllowed(Order order);

        /// <summary>
        /// Validates the order total (excluding discounts).
        /// Gets the mininmum and maximum allowed order amount from <see cref="CustomerRole.OrderTotalMinimum"/> and <see cref="CustomerRole.OrderTotalMaximum"/>
        /// or from <see cref="OrderSettings.OrderTotalMinimum"/> and <see cref="OrderSettings.OrderTotalMaximum"/> if none is specified.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="customerRoles">Customer roles to check against.</param>
        /// <returns>Validated order total.</returns>
        Task<OrderTotalValidationResult> ValidateOrderTotalAsync(IList<OrganizedShoppingCartItem> cart, params CustomerRole[] customerRoles);

        /// <summary>
        /// Adds a shipment to an order.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="trackingNumber">Tracking number.</param>
        /// <param name="trackingUrl">Tracking URL.</param>
        /// <param name="quantities">Quantities by order item identifiers. <c>null</c> to use the remaining total number of products for each order item.</param>
        /// <returns>New shipment, <c>null</c> if no shipment was added.</returns>
        Task<Shipment> AddShipmentAsync(Order order, string trackingNumber, string trackingUrl, Dictionary<int, int> quantities);

        /// <summary>
        /// Auto update order details, e.g. when the user has manually edited order items.
        /// </summary>
        /// <param name="context">Auto update context.</param>
        Task AutoUpdateOrderDetailsAsync(AutoUpdateOrderItemContext context);

        #region Place order

        /// <summary>
        /// Places an order.
        /// </summary>
        /// <param name="paymentRequest">Payment processing request.</param>
        /// <param name="extraData">Additional data to be taken into account when placing the order.</param>
        /// <returns>Order placement result.</returns>
        Task<OrderPlacementResult> PlaceOrderAsync(ProcessPaymentRequest paymentRequest, Dictionary<string, string> extraData);

        /// <summary>
        /// Checks whether an order can be placed.
        /// </summary>
        /// <param name="paymentRequest">Payment processing request.</param>
        /// <param name="initialOrder">Initial order for recurring payment, if any.</param>
        /// <param name="customer">Customer placing the order.</param>
        /// <returns>Warnings and validated card. Warnings are empty if the order can be placed.</returns>
        Task<(IList<string> Warnings, IList<OrganizedShoppingCartItem> Cart)> ValidateOrderPlacementAsync(
            ProcessPaymentRequest paymentRequest,
            Order initialOrder = null,
            Customer customer = null);

        /// <summary>
        /// Checks whether orders are placed in too short time interval.
        /// </summary>
        /// <param name="customer">Customer.</param>
        /// <param name="store">Store.</param>
        /// <returns><c>true</c> valid interval, <c>false</c> invalid interval.</returns>
        Task<bool> IsMinimumOrderPlacementIntervalValidAsync(Customer customer, Store store);

        #endregion

        #region Payment

        /// <summary>
        /// Gets a value indicating whether an order can be marked as authorized.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be marked as authorized.</returns>
        bool CanMarkOrderAsAuthorized(Order order);

        /// <summary>
        /// Marks an order as authorized.
        /// </summary>
        /// <param name="order">Order.</param>
        Task MarkAsAuthorizedAsync(Order order);

        /// <summary>
        /// Gets a value indicating whether an order can be marked as paid.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be marked as paid.</returns>
        bool CanMarkOrderAsPaid(Order order);

        /// <summary>
        /// Marks an order as paid.
        /// </summary>
        /// <param name="order">Order.</param>
        Task MarkOrderAsPaidAsync(Order order);

        /// <summary>
        /// Gets a value indicating whether an order can be marked as paid.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be marked as paid.</returns>
        Task<bool> CanCaptureAsync(Order order);

        /// <summary>
        /// Captures an order.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>List of errors if any.</returns>
        Task<IList<string>> CaptureAsync(Order order);

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
        Task<bool> CanPartiallyRefundAsync(Order order, decimal amountToRefund);

        /// <summary>
        /// Partially refunds an order. The payment is partially refunded at the payment provider if he supports this.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        /// <returns>List of errors if any.</returns>
        Task<IList<string>> PartiallyRefundAsync(Order order, decimal amountToRefund);

        /// <summary>
        /// Gets a value indicating whether an order can be partially refunded "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        /// <returns>A value indicating whether an order can be partially refunded "offline".</returns>
        bool CanPartiallyRefundOffline(Order order, decimal amountToRefund);

        /// <summary>
        /// Partially refunds an order "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        Task PartiallyRefundOfflineAsync(Order order, decimal amountToRefund);

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
        /// Gets a value indicating whether a customer can cancel recurring payment.
        /// </summary>
        /// <param name="customerToValidate">Customer.</param>
        /// <param name="recurringPayment">Recurring payment.</param>
        /// <returns>A value indicating whether a customer can cancel recurring payment.</returns>
        bool CanCancelRecurringPayment(Customer customerToValidate, RecurringPayment recurringPayment);

        /// <summary>
        /// Cancels a recurring payment.
        /// </summary>
        /// <param name="recurringPayment">Recurring payment.</param>
        /// <returns>List of errors if any.</returns>
        Task<IList<string>> CancelRecurringPaymentAsync(RecurringPayment recurringPayment);

        /// <summary>
        /// Processes the next recurring psayment.
        /// </summary>
        /// <param name="recurringPayment">Recurring payment.</param>
        Task ProcessNextRecurringPaymentAsync(RecurringPayment recurringPayment);

        #endregion
    }
}
