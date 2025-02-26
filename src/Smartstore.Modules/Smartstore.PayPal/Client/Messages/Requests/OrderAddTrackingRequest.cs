namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Adds a tracking number to a PayPal order.
    /// </summary>
    public class OrderAddTrackingRequest : PayPalRequest2<OrderAddTrackingRequest, object>
    {
        public OrderAddTrackingRequest(string orderId)
            : base("/v2/checkout/orders/{0}/track", HttpMethod.Post)
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