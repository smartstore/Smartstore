namespace Smartstore.Web.Api.Models.Checkout
{
    /// <summary>
    /// Provides additional shipment information for an order.
    /// </summary>
    public partial class OrderShipmentInfo
    {
        /// <summary>
        /// Gets a value indicating whether an order has items to dispatch.
        /// </summary>
        public bool HasItemsToDispatch { get; set; }

        /// <summary>
        /// Gets a value indicating whether an order has items to deliver.
        /// </summary>
        public bool HasItemsToDeliver { get; set; }

        /// <summary>
        /// Gets a value indicating whether an order has items to be added to a shipment.
        /// </summary>
        public bool CanAddItemsToShipment { get; set; }
    }
}
