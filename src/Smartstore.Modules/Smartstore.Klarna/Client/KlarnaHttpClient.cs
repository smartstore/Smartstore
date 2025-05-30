using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Klarna.Client
{
    public class KlarnaHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly KlarnaApiConfig _apiConfig; // Assuming a config class for API key, secret, and base URL

        public KlarnaHttpClient(HttpClient httpClient, KlarnaApiConfig apiConfig)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiConfig = apiConfig ?? throw new ArgumentNullException(nameof(apiConfig));

            _httpClient.BaseAddress = new Uri(_apiConfig.ApiUrl); // e.g., "https://api.klarna.com/"
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Basic Authentication: username:password -> base64
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiConfig.ApiKey}:{_apiConfig.ApiSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        // Example: Create Credit Session
        public async Task<CreateSessionResponse> CreateCreditSessionAsync(CreateSessionRequest request)
        {
            var requestUri = "payments/v1/sessions"; // Example endpoint
            return await SendRequestAsync<CreateSessionResponse>(HttpMethod.Post, requestUri, request);
        }

        // Example: Update Credit Session
        public async Task UpdateCreditSessionAsync(string sessionId, UpdateSessionRequest request)
        {
            var requestUri = $"payments/v1/sessions/{sessionId}"; // Example endpoint
            await SendRequestAsync<object>(HttpMethod.Post, requestUri, request); // No specific response body for update, or define one if needed
        }

        // Example: Get Session Details
        public async Task<SessionDetailsResponse> GetCreditSessionAsync(string sessionId)
        {
            var requestUri = $"payments/v1/sessions/{sessionId}"; // Example endpoint
            return await SendRequestAsync<SessionDetailsResponse>(HttpMethod.Get, requestUri);
        }

        // Example: Create Order
        public async Task<CreateOrderResponse> CreateOrderAsync(string authorizationToken, CreateOrderRequest request)
        {
            var requestUri = $"ordermanagement/v1/orders"; // Example endpoint for Klarna Order Management API
            // For order creation, Klarna might require the authorization_token from the payment session
            // This might need a different setup or HttpClient instance if the base URL or auth changes for OMS API
            return await SendRequestAsync<CreateOrderResponse>(HttpMethod.Post, requestUri, request);
        }

        // Example: Generate Customer Token
        public async Task<CustomerTokenResponse> GenerateCustomerTokenAsync(CreateCustomerTokenRequest request)
        {
            var requestUri = "customer-token/v1/tokens"; // Example endpoint
            return await SendRequestAsync<CustomerTokenResponse>(HttpMethod.Post, requestUri, request);
        }

        private async Task<TResponse> SendRequestAsync<TResponse>(HttpMethod method, string requestUri, object requestData = null)
        {
            var requestMessage = new HttpRequestMessage(method, requestUri);

            if (requestData != null)
            {
                var jsonRequest = JsonConvert.SerializeObject(requestData);
                requestMessage.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                var errorContent = await responseMessage.Content.ReadAsStringAsync();
                // TODO: Deserialize errorContent into a KlarnaError object if Klarna provides a standard error format
                throw new KlarnaApiException($"Klarna API request failed with status code {responseMessage.StatusCode}: {errorContent}");
            }

            var jsonResponse = await responseMessage.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse) && typeof(TResponse) != typeof(object))
            {
                // Handle cases where Klarna might return an empty body for success (e.g., 204 No Content)
                // and TResponse is not expecting it (e.g. not 'object' or a specific 'EmptyResponse' type)
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default; // Or throw, or return a specific object indicating success with no content
                }
                throw new KlarnaApiException("Received an empty response from Klarna API when content was expected.");
            }

            // If TResponse is object, it means we don't expect a specific response body or it's handled by status code.
            if (typeof(TResponse) == typeof(object))
            {
                return default; // Or a new object(), depending on how you want to signal this.
            }

            try
            {
                return JsonConvert.DeserializeObject<TResponse>(jsonResponse);
            }
            catch (JsonException ex)
            {
                throw new KlarnaApiException($"Failed to deserialize Klarna API response: {ex.Message}", ex);
            }
        }
    }
}
