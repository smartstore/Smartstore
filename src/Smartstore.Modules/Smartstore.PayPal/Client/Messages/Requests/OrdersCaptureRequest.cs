namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Captures a payment for an order.
    /// </summary>
    public class OrdersCaptureRequest : PayPalRequest<object>
    {
        public OrdersCaptureRequest(string orderId)
            : base("/v2/checkout/orders/{0}/capture?", HttpMethod.Post)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(orderId));
            }
            catch (IOException)
            {
            }

            ContentType = "application/json";
        }

        public OrdersCaptureRequest WithClientMetadataId(string payPalClientMetadataId)
        {
            Headers.Add("PayPal-Client-Metadata-Id", payPalClientMetadataId);
            return this;
        }

        public OrdersCaptureRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public OrdersCaptureRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }
    }
}
