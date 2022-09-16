namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a PayPal order.
    /// </summary>
    public class OrderCreateRequest : PayPalRequest<object>
    {
        public OrderCreateRequest()
            : base("/v2/checkout/orders", HttpMethod.Post)
        {
            ContentType = "application/json";
        }

        public OrderCreateRequest WithClientMetadataId(string payPalClientMetadataId)
        {
            Headers.Add("PayPal-Client-Metadata-Id", payPalClientMetadataId);
            return this;
        }

        public OrderCreateRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public OrderCreateRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public OrderCreateRequest WithBody(OrderMessage webhook)
        {
            Body = webhook;
            return this;
        }
    }
}
