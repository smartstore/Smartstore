using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartStore.DellyManLogistics.Models
{
    public class DellyManWebhookOrder
    {
        [JsonPropertyName("OrderID")]
        public string OrderID { get; set; }

        [JsonPropertyName("OrderCode")]
        public string OrderCode { get; set; }

        [JsonPropertyName("CustomerID")]
        public string CustomerID { get; set; }

        [JsonPropertyName("CompanyID")]
        public string CompanyID { get; set; }

        [JsonPropertyName("TrackingID")]
        public string TrackingID { get; set; }

        [JsonPropertyName("OrderDate")]
        public string OrderDate { get; set; }

        [JsonPropertyName("OrderStatus")]
        public string OrderStatus { get; set; }

        [JsonPropertyName("OrderPrice")]
        public string OrderPrice { get; set; }

        [JsonPropertyName("AssignedAt")]
        public string AssignedAt { get; set; }

        [JsonPropertyName("PickedUpAt")]
        public string PickedUpAt { get; set; }

        [JsonPropertyName("DeliveredAt")]
        public string DeliveredAt { get; set; }
    }
}
