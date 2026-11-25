using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class CreateSessionResponse : KlarnaApiResponse
    {
        // Properties based on Klarna API response for creating a session
        // e.g., SessionId, ClientToken, PaymentMethodCategories etc.
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("client_token")]
        public string ClientToken { get; set; }
    }
}
