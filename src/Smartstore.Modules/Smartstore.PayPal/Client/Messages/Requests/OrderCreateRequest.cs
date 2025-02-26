namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a PayPal order.
    /// </summary>
    public class OrderCreateRequest : PayPalRequest2<OrderCreateRequest, OrderMessage>
    {
        public OrderCreateRequest()
            : base("/v2/checkout/orders", HttpMethod.Post)
        {
        }
    }
}
