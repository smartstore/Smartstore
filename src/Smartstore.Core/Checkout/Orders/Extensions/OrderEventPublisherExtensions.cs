using System.Threading.Tasks;
using Smartstore.Core.Checkout.Orders.Events;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public static class OrderEventPublisherExtensions
    {
        /// <summary>
        /// Publishes the order paid event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="order">The order instance.</param>
        public static Task PublishOrderPaid(this IEventPublisher eventPublisher, Order order)
        {
            if (order != null)
                return eventPublisher.PublishAsync(new OrderPaidEvent(order));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Publishes the order placed event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="order">The order instance.</param>
        public static Task PublishOrderPlaced(this IEventPublisher eventPublisher, Order order)
        {
            if (order != null)
                eventPublisher.PublishAsync(new OrderPlacedEvent(order));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Publishes the order updated event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="order">The order instance.</param>
        public static Task PublishOrderUpdated(this IEventPublisher eventPublisher, Order order)
        {
            if (order != null)
                eventPublisher.PublishAsync(new OrderUpdatedEvent(order));

            return Task.CompletedTask;
        }
    }
}