namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets merchant status.
    /// </summary>
    public class GetMerchantStatusRequest : PayPalRequest<MerchantStatus>
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

            ContentType = "application/json";
        }

        public GetMerchantStatusRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }
    }
}