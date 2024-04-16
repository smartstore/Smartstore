namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Updates a tracking number of a PayPal order.
    /// </summary>
    public class OrderUpdateTrackingRequest : PayPalRequest<object>
    {
        public OrderUpdateTrackingRequest(string orderId, string trackerId)
            : base("/v2/checkout/orders/{0}/trackers/{1}", HttpMethod.Patch)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(orderId), Uri.EscapeDataString(trackerId));
            }
            catch (IOException)
            {
            }

            ContentType = "application/json";
        }

        public OrderUpdateTrackingRequest WithBody(List<Patch<string>> patchRequest)
        {
            Body = patchRequest;
            return this;
        }
    }
}