namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Authorizes payment for an order. The response shows details of authorizations. 
    /// You can make this call only if you specified `intent=AUTHORIZE` in the create order call.
    /// </summary>
    public class OrdersAuthorizeRequest : PayPalRequest<object>
    {
        public OrdersAuthorizeRequest(string captureId)
            : base("/v2/checkout/orders/{0}/authorize?", HttpMethod.Post)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(captureId));
            }
            catch (IOException)
            {
            }

            ContentType = "application/json";
        }

        public OrdersAuthorizeRequest WithClientMetadataId(string payPalClientMetadataId)
        {
            Headers.Add("PayPal-Client-Metadata-Id", payPalClientMetadataId);
            return this;
        }

        public OrdersAuthorizeRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public OrdersAuthorizeRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }
    }
}
