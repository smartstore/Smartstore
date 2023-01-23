using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartStore.DellyManLogistics.Models
{
    public class DellyManBookOrderModel
    {
        public DellyManBookOrderModel()
        {
            Packages = new List<DellyManPackageModel>();
        }
        [JsonPropertyName("CustomerID")]
        public int CustomerID { get; set; }

        [JsonPropertyName("CompanyID")]
        public int CompanyID { get; set; }

        [JsonPropertyName("PaymentMode")]
        public string PaymentMode { get; set; }

        [JsonPropertyName("OrderRef")]
        public string OrderRef { get; set; }

        [JsonPropertyName("Vehicle")]
        public string Vehicle { get; set; }

        [JsonPropertyName("PickUpContactName")]
        public string PickUpContactName { get; set; }

        [JsonPropertyName("PickUpContactNumber")]
        public string PickUpContactNumber { get; set; }

        [JsonPropertyName("PickUpGooglePlaceAddress")]
        public string PickUpGooglePlaceAddress { get; set; }

        [JsonPropertyName("PickUpLandmark")]
        public string PickUpLandmark { get; set; }

        [JsonPropertyName("IsInstantDelivery")]
        public int IsInstantDelivery { get; set; }

        [JsonPropertyName("PickUpRequestedDate")]
        public string PickUpRequestedDate { get; set; }

        [JsonPropertyName("PickUpRequestedTime")]
        public string PickUpRequestedTime { get; set; }

        [JsonPropertyName("DeliveryRequestedTime")]
        public string DeliveryRequestedTime { get; set; }

        [JsonPropertyName("DeliveryRequestedDate")]
        public string DeliveryRequestedDate { get; set; }

        [JsonPropertyName("Packages")]
        public List<DellyManPackageModel> Packages { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
