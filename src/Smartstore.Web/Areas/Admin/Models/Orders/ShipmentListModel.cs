using System;
using Smartstore.Web.Modelling;

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

        public int? OrderId { get; set; }
        public bool DisplayPdfPackagingSlip { get; set; }
    }
}
