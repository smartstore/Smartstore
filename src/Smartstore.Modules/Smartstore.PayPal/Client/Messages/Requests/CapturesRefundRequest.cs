namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Refunds a captured payment, by ID. For a full refund, include an empty payload in the JSON request body. 
    /// For a partial refund, include an <code>amount</code> object in the JSON request body.
    /// </summary>
    public class CapturesRefundRequest : PayPalRequest2<CapturesRefundRequest, RefundMessage>
    {
        public CapturesRefundRequest(string captureId)
            : base("/v2/payments/captures/{0}/refund?", HttpMethod.Post)
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
