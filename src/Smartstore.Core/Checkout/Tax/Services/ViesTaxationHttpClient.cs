using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Smartstore.Core.Checkout.Tax
{
    public class ViesTaxationHttpClient
    {
        private readonly HttpClient _httpClient;

        public ViesTaxationHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CheckVatNumberResponseMessage> CheckVatAsync(string vatNumber, string countryCode, CancellationToken cancelToken = default)
        {
            var url = "https://ec.europa.eu/taxation_customs/vies/rest-api/check-vat-number";

            var request = new CheckVatNumberRequestMessage
            {
                CountryCode = countryCode,
                VatNumber = vatNumber
            };

            var jsonData = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cancelToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancelToken);
                var message = JsonConvert.DeserializeObject<CheckVatNumberResponseMessage>(responseContent);

                if (message.ActionSucceed.HasValue && message.ActionSucceed == false)
                {
                    throw new Exception($"EU tax service returned an error: {message.ErrorWrappers?.FirstOrDefault()?.ErrorMessage}.");
                }

                return message;
            }
            else
            {
                throw new Exception($"EU tax service returned status code {response.StatusCode}.");
            }
        }
    }

    public class CheckVatNumberRequestMessage
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