using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;

namespace Smartstore
{
    public static partial class OrderExtensions
    {
        /// <summary>
        /// Gets <see cref="Order.OrderNumber"/> or <see cref="BaseEntity.Id"/> if empty.
        /// </summary>
        /// <returns><see cref="Order.OrderNumber"/> or <see cref="BaseEntity.Id"/> if empty.</returns>
        public static string GetOrderNumber(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            return order.OrderNumber.IsEmpty() ? order.Id.ToString() : order.OrderNumber;
        }

        /// <summary>
        /// Gets a value indicating whether an order can be marked as completed.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be marked as completed.</returns>
        public static bool CanCompleteOrder(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            return order.OrderStatus != OrderStatus.Complete && order.OrderStatus != OrderStatus.Cancelled;
        }

        /// <summary>
        /// Gets a value indicating whether an order can be cancelled.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be cancelled.</returns>
        public static bool CanCancelOrder(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            return order.OrderStatus != OrderStatus.Cancelled;
        }

        /// <summary>
        /// Adds an order note. The caller is responsible for database commit.
        /// </summary>
		/// <param name="order">Order entity.</param>
        /// <param name="note">Note to add.</param>
        /// <param name="displayToCustomer">A value indicating whether to display the note to the customer.</param>
        public static void AddOrderNote(this Order order, string note, bool displayToCustomer = false)
        {
            if (order != null && note.HasValue())
            {
                order.OrderNotes.Add(new OrderNote
                {
                    Note = note,
                    DisplayToCustomer = displayToCustomer,
                    CreatedOnUtc = DateTime.UtcNow
                });
            }
        }

        #region Payment

        /// <summary>
        /// Gets a value indicating whether an order can be marked as authorized.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be marked as authorized.</returns>
        public static bool CanMarkOrderAsAuthorized(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (order.PaymentStatus == PaymentStatus.Pending)
                return true;

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether an order can be marked as paid.
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be marked as paid.</returns>
        public static bool CanMarkOrderAsPaid(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.Refunded ||
                order.PaymentStatus == PaymentStatus.Voided)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a value indicating whether an order can be refunded "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be refunded.</returns>
        public static bool CanRefundOffline(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Only partial refunds allowed if already refunded.
            if (order.RefundedAmount > decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //     return false;

            if (order.PaymentStatus == PaymentStatus.Paid)
                return true;

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether an order can be partially refunded "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <param name="amountToRefund">The amount to refund.</param>
        /// <returns>A value indicating whether an order can be partially refunded "offline".</returns>
        public static bool CanPartiallyRefundOffline(this Order order, decimal amountToRefund)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            var canBeRefunded = order.OrderTotal - order.RefundedAmount;
            if (canBeRefunded <= decimal.Zero)
                return false;

            if (amountToRefund > canBeRefunded)
                return false;

            if (order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.PartiallyRefunded)
                return true;

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether an order can be voided "offline" (without calling any payment provider).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns>A value indicating whether an order can be voided "offline".</returns>
        public static bool CanVoidOffline(this Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            if (order.PaymentStatus == PaymentStatus.Authorized)
                return true;

            return false;
        }

        #endregion
    }
}
