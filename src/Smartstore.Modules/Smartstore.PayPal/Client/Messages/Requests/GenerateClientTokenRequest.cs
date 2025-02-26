namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Generates a client token (mandatory for presenting credit card processing in hosted fields).
    /// </summary>
    public class GenerateClientTokenRequest : PayPalRequest
    {
        public GenerateClientTokenRequest()
            : base("/v1/identity/generate-token", HttpMethod.Post)
        {
        }
    }
}
