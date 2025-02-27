using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets merchant status.
    /// </summary>
    public class GetMerchantStatusRequest : PayPalRequest<GetMerchantStatusRequest, MerchantStatus>
    {
        public GetMerchantStatusRequest(string partnerId, string payerId)
            : base(FormatPath("/v1/customer/partners/{0}/merchant-integrations/{1}", partnerId, payerId), HttpMethod.Get)
        {
        }
    }

    /// <summary>
    /// Gets seller credentials.
    /// </summary>
    public class GetSellerCredentialsRequest : PayPalRequest<GetSellerCredentialsRequest, SellerCredentials>
    {
        public GetSellerCredentialsRequest(string partnerId, string token)
            : base(FormatPath("/v1/customer/partners/{0}/merchant-integrations/credentials", partnerId), HttpMethod.Get)
        {
            Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
