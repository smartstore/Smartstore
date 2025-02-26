namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Updates an order that has the `CREATED` or `APPROVED` status. You cannot update an order with `COMPLETED` status.
    /// </summary>
    public class OrdersPatchRequest<TPatch> : PayPalRequest2<OrdersPatchRequest<TPatch>, List<Patch<TPatch>>>
    {
        public OrdersPatchRequest(string orderId)
            : base("/v2/checkout/orders/{0}?", HttpMethod.Patch)
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
