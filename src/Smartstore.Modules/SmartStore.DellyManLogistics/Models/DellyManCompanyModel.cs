using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartStore.DellyManLogistics.Models
{
    public class DellyManCompanyModel
    {
        [JsonPropertyName("CompanyID")]
        public int CompanyID { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("TotalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("OriginalPrice")]
        public int OriginalPrice { get; set; }

        [JsonPropertyName("SavedPrice")]
        public int SavedPrice { get; set; }

        [JsonPropertyName("PayablePrice")]
        public int PayablePrice { get; set; }

        [JsonPropertyName("DeductablePrice")]
        public int DeductablePrice { get; set; }

        [JsonPropertyName("AvgRating")]
        public int AvgRating { get; set; }

        [JsonPropertyName("NumberOfOrders")]
        public int NumberOfOrders { get; set; }

        [JsonPropertyName("NumberOfRating")]
        public int NumberOfRating { get; set; }
    }
}
