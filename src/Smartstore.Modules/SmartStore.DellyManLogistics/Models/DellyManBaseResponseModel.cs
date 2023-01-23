using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartStore.DellyManLogistics.Models
{
    public class DellyManBaseResponseModel<T>
    {
        [JsonPropertyName("ResponseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("ResponseMessage")]
        public string ResponseMessage { get; set; }

        [JsonPropertyName("Companies")]
        public T Data { get; set; }

        [JsonPropertyName("RejectedCompanies")]
        public DellyManRejectedCompany RejectedCompanies { get; set; }

        [JsonPropertyName("Products")]
        public DellyManProduct Products { get; set; }

        [JsonPropertyName("Distance")]
        public int Distance { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }


    public class DellyManRejectedCompany
    {

    }

    public class DellyManProduct
    {

    }
}
