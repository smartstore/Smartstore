using System.Net.Http;
using Newtonsoft.Json;

namespace Smartstore.Core.Checkout.Tax.Domain
{
    public class CheckVatNumberRequestMessage : HttpRequestMessage
    {
        [JsonProperty("countryCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CountryCode;

        [JsonProperty("vatNumber", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string VatNumber;

        [JsonProperty("requesterMemberStateCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RequesterMemberStateCode;

        [JsonProperty("requesterNumber", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RequesterNumber;

        [JsonProperty("traderName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderName;

        [JsonProperty("traderStreet", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderStreet;

        [JsonProperty("traderPostalCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderPostalCode;

        [JsonProperty("traderCity", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderCity;

        [JsonProperty("traderCompanyType", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderCompanyType;
    }
}
