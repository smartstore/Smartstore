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
            // TODO: (mh) Please perform live checks. It always returns MS_UNAVAILABLE during unit tests.
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
}