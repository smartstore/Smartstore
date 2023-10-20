using Newtonsoft.Json;

namespace Smartstore.Core.Checkout.Tax.Domain
{
    public class CheckVatNumberResponseMessage
    {
        [JsonProperty("countryCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CountryCode;

        [JsonProperty("vatNumber", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string VatNumber;

        [JsonProperty("requestDate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime RequestDate;

        [JsonProperty("valid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsValid;

        [JsonProperty("requestIdentifier", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RequestIdentifier;

        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name;

        [JsonProperty("Address", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Address;

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

        [JsonProperty("traderNameMatch", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderNameMatch;

        [JsonProperty("traderStreetMatch", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderStreetMatch;

        [JsonProperty("traderPostalCodeMatch", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderPostalCodeMatch;

        [JsonProperty("traderCityMatch", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderCityMatch;

        [JsonProperty("traderCompanyTypeMatch", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TraderCompanyTypeMatch;

        [JsonProperty("actionSucceed", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? ActionSucceed;

        [JsonProperty("errorWrappers", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<VatServiceError> ErrorWrappers;
    }

    public class VatServiceError
    {
        [JsonProperty("error", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ErrorMessage;
    }
}