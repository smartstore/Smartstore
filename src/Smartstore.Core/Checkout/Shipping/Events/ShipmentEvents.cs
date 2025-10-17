using Smartstore.Events;

namespace Smartstore.Core.Checkout.Shipping.Events
{
    public class TrackingNumberAddedEvent(Shipment shipment) : IEventMessage
    {
        public Shipment Shipment { get; init; } = shipment;
    }

    public class TrackingNumberChangedEvent(Shipment shipment, string oldTrackingNumber) : IEventMessage
    {
        public string OldTrackingNumber { get; init; } = oldTrackingNumber;
        public Shipment Shipment { get; init; } = shipment;
    }
}