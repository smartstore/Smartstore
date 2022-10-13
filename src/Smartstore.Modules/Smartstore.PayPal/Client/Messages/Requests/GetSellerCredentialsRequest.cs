using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets seller credentials.
    /// </summary>
    public class GetSellerCredentialsRequest : PayPalRequest<SellerCredentials>
    {
        public GetSellerCredentialsRequest(string partnerId, string token)
            : base("/v1/customer/partners/{0}/merchant-integrations/credentials", HttpMethod.Get)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(partnerId));
            }
            catch (IOException)
            {
            }

            ContentType = "application/json";

            Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public GetSellerCredentialsRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }
    }
}