using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Smartstore.Json;

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

            var jsonOptions = SmartJsonOptions.CamelCasedIgnoreDefaultValue;
            var response = await _httpClient.PostAsJsonAsync(url, request, jsonOptions, cancelToken);

            if (response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadFromJsonAsync<CheckVatNumberResponseMessage>(jsonOptions, cancelToken);
                if (message.ActionSucceed == false)
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
        public string CountryCode;
        public string VatNumber;
        public string RequesterMemberStateCode;
        public string RequesterNumber;
        public string TraderName;
        public string TraderStreet;
        public string TraderPostalCode;
        public string TraderCity;
        public string TraderCompanyType;
    }

    public class CheckVatNumberResponseMessage
    {
        public string CountryCode;
        public string VatNumber;
        public DateTime? RequestDate;
        [JsonPropertyName("valid")]
        public bool IsValid;
        public string RequestIdentifier;
        public string Name;
        public string Address;
        public string TraderName;
        public string TraderStreet;
        public string TraderPostalCode;
        public string TraderCity;
        public string TraderCompanyType;
        public string TraderNameMatch;
        public string TraderStreetMatch;
        public string TraderPostalCodeMatch;
        public string TraderCityMatch;
        public string TraderCompanyTypeMatch;
        public bool? ActionSucceed;
        public List<VatServiceError> ErrorWrappers;
    }

    public class VatServiceError
    {
        [JsonPropertyName("error")]
        public string ErrorMessage;
    }
}