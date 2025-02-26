namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Captures an authorized order, by ID.
    /// </summary>
    public class AuthorizationsCaptureRequest : PayPalRequest2<AuthorizationsCaptureRequest, CaptureMessage>
    {
        public AuthorizationsCaptureRequest(string captureId)
            : base("/v2/payments/authorizations/{0}/capture?", HttpMethod.Post)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(captureId));
            }
            catch (IOException)
            {
            }
        }
    }
}
