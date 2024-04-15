using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Shipping.Events;
using Smartstore.Events;

namespace Smartstore
{
    public static class ShipmentEventPublisherExtensions
    {
        /// <summary>
        /// Publishes the tracking number added event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="shipment">The shipment the tracking number that was added to.</param>
        public static Task PublishTrackingNumberAddedAsync(this IEventPublisher eventPublisher, Shipment shipment)
        {
            return shipment != null
                ? eventPublisher.PublishAsync(new TrackingNumberAddedEvent(shipment))
                : Task.CompletedTask;
        }

        /// <summary>
        /// Publishes the tracking number changed event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="shipment">The shipment for which the tracking number has changed.</param>
        /// <param name="oldTrackingNumber">The old tracking number of the shipment.</param>
        public static Task PublishTrackingNumberChangedAsync(this IEventPublisher eventPublisher, Shipment shipment, string oldTrackingNumber)
        {
            return shipment != null
                ? eventPublisher.PublishAsync(new TrackingNumberChangedEvent(shipment, oldTrackingNumber))
                : Task.CompletedTask;
        }
    }
}
