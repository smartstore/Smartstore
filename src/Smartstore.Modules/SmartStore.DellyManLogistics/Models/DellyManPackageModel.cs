using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartStore.DellyManLogistics.Models
{
    public class DellyManPackageModel
    {
        [JsonPropertyName("PackageDescription")]
        public string PackageDescription { get; set; }

        [JsonPropertyName("DeliveryContactName")]
        public string DeliveryContactName { get; set; }

        [JsonPropertyName("DeliveryContactNumber")]
        public string DeliveryContactNumber { get; set; }

        [JsonPropertyName("DeliveryGooglePlaceAddress")]
        public string DeliveryGooglePlaceAddress { get; set; }

        [JsonPropertyName("DeliveryLandmark")]
        public string DeliveryLandmark { get; set; }

        [JsonPropertyName("PickUpState")]
        public string PickUpState { get; set; }

        [JsonPropertyName("PickUpCity")]
        public string PickUpCity { get; set; }

        [JsonPropertyName("DeliveryState")]
        public string DeliveryState { get; set; }

        [JsonPropertyName("DeliveryCity")]
        public string DeliveryCity { get; set; }
    }
}
