using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartStore.DellyManLogistics.Models
{
    public class DellyManBookOrderResponseModel
    {
        [JsonPropertyName("ResponseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("ResponseMessage")]
        public string ResponseMessage { get; set; }

        [JsonPropertyName("OrderID")]
        public long OrderID { get; set; }

        [JsonPropertyName("TrackingID")]
        public long TrackingID { get; set; }

        [JsonPropertyName("Reference")]
        public string Reference { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
