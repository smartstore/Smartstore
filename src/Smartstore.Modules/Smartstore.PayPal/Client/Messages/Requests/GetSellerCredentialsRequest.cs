using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets seller credentials.
    /// </summary>
    public class GetSellerCredentialsRequest : PayPalRequest2<GetSellerCredentialsRequest, SellerCredentials>
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

            Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}