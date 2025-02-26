namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Captures a payment for an order.
    /// </summary>
    public class OrdersCaptureRequest : PayPalRequest2<OrdersCaptureRequest, object>
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
        }
    }
}
