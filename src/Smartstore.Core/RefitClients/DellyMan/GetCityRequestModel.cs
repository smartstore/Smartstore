using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Smartstore.Core.RefitClients.DellyMan
{
    public class GetCityRequestModel
    {
        [JsonPropertyName("StateID")]
        public int StateId { get; set; }
    }
}
