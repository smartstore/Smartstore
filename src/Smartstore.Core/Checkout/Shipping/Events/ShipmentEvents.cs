namespace Smartstore.Core.Checkout.Shipping.Events
{
    public class TrackingNumberAddedEvent
    {
        public TrackingNumberAddedEvent(Shipment shipment)
        {
            Shipment = shipment;
        }

        public Shipment Shipment { get; init; }
    }

    public class TrackingNumberChangedEvent
    {
        public TrackingNumberChangedEvent(Shipment shipment, string oldTrackingNumber)
        {
            Shipment = shipment;
            OldTrackingNumber = oldTrackingNumber;
        }

        public string OldTrackingNumber { get; init; }
        public Shipment Shipment { get; init; }
    }
}