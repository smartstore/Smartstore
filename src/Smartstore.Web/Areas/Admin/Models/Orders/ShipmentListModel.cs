namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Shipments.List.")]
    public class ShipmentListModel : ModelBase
    {
        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("*TrackingNumber")]
        public string TrackingNumber { get; set; }

        [LocalizedDisplay("Admin.Orders.Fields.ShippingMethod")]
        public string ShippingMethod { get; set; }

        public int? OrderId { get; set; }
    }
}
