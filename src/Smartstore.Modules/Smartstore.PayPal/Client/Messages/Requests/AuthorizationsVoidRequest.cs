namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Voids or cancels an authorized payment, by ID. You cannot void an authorized payment that has been fully captured.
    /// </summary>
    public class AuthorizationsVoidRequest : PayPalRequest<object>
    {
        public AuthorizationsVoidRequest(string authorizationId)
            : base("/v2/payments/authorizations/{0}/void?", HttpMethod.Post)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(authorizationId));
            }
            catch (IOException)
            {
            }

            ContentType = "application/json";
        }
    }
}
