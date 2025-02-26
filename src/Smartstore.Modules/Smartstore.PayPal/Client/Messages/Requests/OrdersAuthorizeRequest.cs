namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Authorizes payment for an order. The response shows details of authorizations. 
    /// You can make this call only if you specified `intent=AUTHORIZE` in the create order call.
    /// </summary>
    public class OrdersAuthorizeRequest : PayPalRequest2<OrdersAuthorizeRequest, object>
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
        }
    }
}
