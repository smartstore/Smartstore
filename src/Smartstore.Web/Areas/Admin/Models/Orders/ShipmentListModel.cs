namespace Smartstore.Admin.Models.Orders
{
    public class ShipmentListModel : ModelBase
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("Admin.Orders.Shipments.TrackingNumber")]
        public string TrackingNumber { get; set; }

        [LocalizedDisplay("Admin.Orders.Fields.ShippingMethod")]
        public string ShippingMethod { get; set; }

        public int? OrderId { get; set; }
    }
}
