using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Events;
using Smartstore.Events;

namespace Smartstore
{
    public static class OrderEventPublisherExtensions
    {
        /// <summary>
        /// Publishes the order paid event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="order">The order instance.</param>
        public static Task PublishOrderPaidAsync(this IEventPublisher eventPublisher, Order order)
        {
            return order != null
                ? eventPublisher.PublishAsync(new OrderPaidEvent(order))
                : Task.CompletedTask;
        }

        /// <summary>
        /// Publishes the order placed event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="order">The order instance.</param>
        public static Task PublishOrderPlacedAsync(this IEventPublisher eventPublisher, Order order)
        {
            return order != null
                ? eventPublisher.PublishAsync(new OrderPlacedEvent(order))
                : Task.CompletedTask;
        }

        /// <summary>
        /// Publishes the order updated event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="order">The order instance.</param>
        public static Task PublishOrderUpdatedAsync(this IEventPublisher eventPublisher, Order order)
        {
            return order != null
                ? eventPublisher.PublishAsync(new OrderUpdatedEvent(order))
                : Task.CompletedTask;
        }
    }
}