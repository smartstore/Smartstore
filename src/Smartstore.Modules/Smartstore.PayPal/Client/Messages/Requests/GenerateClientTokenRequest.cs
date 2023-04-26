namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Generates a client token (mandatory for presenting credit card processing in hosted fields).
    /// </summary>
    public class GenerateClientTokenRequest : PayPalRequest<object>
    {
        public GenerateClientTokenRequest()
            : base("/v1/identity/generate-token", HttpMethod.Post)
        {
            ContentType = "application/json";
        }
    }
}
