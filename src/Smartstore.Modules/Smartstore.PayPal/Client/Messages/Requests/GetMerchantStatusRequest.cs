namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets merchant status.
    /// </summary>
    public class GetMerchantStatusRequest : PayPalRequest2<GetMerchantStatusRequest, MerchantStatus>
    {
        public GetMerchantStatusRequest(string partnerId, string payerId)
            : base("/v1/customer/partners/{0}/merchant-integrations/{1}", HttpMethod.Get)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(partnerId), Uri.EscapeDataString(payerId));
            }
            catch (IOException)
            {
            }
        }
    }
}