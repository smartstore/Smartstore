namespace Smartstore.Web.Api.Models.Checkout
{
    /// <summary>
    /// Provides additional shipment information for an order item.
    /// </summary>
    public partial class OrderItemShipmentInfo
    {
        /// <summary>
        /// Gets the total number of items which can be added to new shipments.
        /// </summary>
        public int ItemsCanBeAddedToShipmentCount { get; set; }

        /// <summary>
        /// Gets the total number of items in all shipments.
        /// </summary>
        public int ShipmentItemsCount { get; set; }

        /// <summary>
        /// Gets the total number of dispatched items.
        /// </summary>
        public int DispatchedItemsCount { get; set; }

        /// <summary>
        /// Gets the total number of not dispatched items.
        /// </summary>
        public int NotDispatchedItemsCount { get; set; }

        /// <summary>
        /// Gets the total number of already delivered items.
        /// </summary>
        public int DeliveredItemsCount { get; set; }

        /// <summary>
        /// Gets the total number of not delivered items.
        /// </summary>
        public int NotDeliveredItemsCount { get; set; }
    }
}
