using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Smartstore.Paystack.Models
{
    public class PaystackInitializeRequestModel
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }

        [JsonPropertyName("callback_url")]
        public string CallBackUrl { get; set; }

    }

}
