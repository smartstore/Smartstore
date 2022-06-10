namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Shows details for an order by ID.
    /// </summary>
    public class OrdersGetRequest : PayPalRequest<object>
    {
        public OrdersGetRequest(string orderId)
            : base("/v2/checkout/orders/{0}?", HttpMethod.Get)
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
    }
}
