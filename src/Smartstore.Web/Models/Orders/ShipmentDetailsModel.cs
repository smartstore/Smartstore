using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Orders
{
    public partial class ShipmentDetailsModel : EntityModelBase
    {
        public string TrackingNumber { get; set; }
        public string TrackingNumberUrl { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public List<ShipmentStatusEventModel> ShipmentStatusEvents { get; set; } = new();
        public bool ShowSku { get; set; }
        public List<ShipmentItemModel> Items { get; set; } = new();

        public OrderDetailsModel Order { get; set; }

        public partial class ShipmentItemModel : EntityModelBase
        {
            public string Sku { get; set; }
            public int ProductId { get; set; }
            public LocalizedValue<string> ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
            public string AttributeInfo { get; set; }

            public int QuantityOrdered { get; set; }
            public int QuantityShipped { get; set; }
        }

        public partial class ShipmentStatusEventModel : ModelBase
        {
            public string EventName { get; set; }
            public string Location { get; set; }
            public string Country { get; set; }
            public DateTime? Date { get; set; }
        }
    }
}
