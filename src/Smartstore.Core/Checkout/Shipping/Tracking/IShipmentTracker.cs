using Smartstore.Core.Checkout.Shipping.Events;

namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Shipment tracker interface
    /// </summary>
    public partial interface IShipmentTracker
    {
        /// <summary>
        /// Checks whether the current tracker can track the number
        /// </summary>
        /// <param name="trackingNumber">The number to track</param>
        /// <returns><c>True</c> if the tracker can track, <c>false</c> otherwise</returns>
        bool IsMatch(string trackingNumber);

        /// <summary>
        /// Gets the url for the tracking info page (third party tracking page)
        /// </summary>
        /// <param name="trackingNumber">The number to track</param>
        /// <returns>The url of the tracking page</returns>
        string GetUrl(string trackingNumber);

        /// <summary>
        /// Gets all shipment events for a tracking number
        /// </summary>
        /// <param name="trackingNumber">The number to track</param>
        /// <returns>Shipment status events</returns>
        Task<List<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber);
    }
}