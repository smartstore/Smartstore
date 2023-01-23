using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartStore.DellyManLogistics.Models
{
    public class GetQuoteRequestModel
    {
        [JsonPropertyName("CustomerID")]
        public int CustomerID { get; set; }

        [JsonPropertyName("PaymentMode")]
        public string PaymentMode { get; set; }

        [JsonPropertyName("VehicleID")]
        public int VehicleID { get; set; }

        [JsonPropertyName("PickupRequestedTime")]
        public string PickupRequestedTime { get; set; }

        [JsonPropertyName("PickupRequestedDate")]
        public string PickupRequestedDate { get; set; }

        [JsonPropertyName("PickupAddress")]
        public string PickupAddress { get; set; }

        [JsonPropertyName("DeliveryAddress")]
        public List<string> DeliveryAddress { get; set; }

        [JsonPropertyName("ProductAmount")]
        public List<decimal> ProductAmount { get; set; }

        [JsonPropertyName("PackageWeight")]
        public List<int> PackageWeight { get; set; }

        [JsonPropertyName("IsProductOrder")]
        public int IsProductOrder { get; set; }

        [JsonPropertyName("IsProductInsurance")]
        public int IsProductInsurance { get; set; }

        [JsonPropertyName("InsuranceAmount")]
        public int InsuranceAmount { get; set; }

        [JsonPropertyName("IsInstantDelivery")]
        public int IsInstantDelivery { get; set; }
    }
}
