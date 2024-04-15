namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Adds a tracking number to a PayPal order.
    /// </summary>
    public class OrderAddTrackingRequest : PayPalRequest<object>
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

            ContentType = "application/json";
        }

        public OrderAddTrackingRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public OrderAddTrackingRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public OrderAddTrackingRequest WithBody(TrackingMessage tracking)
        {
            Body = tracking;
            return this;
        }
    }
}