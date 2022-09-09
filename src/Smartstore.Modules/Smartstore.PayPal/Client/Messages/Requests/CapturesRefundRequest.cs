namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Refunds a captured payment, by ID. For a full refund, include an empty payload in the JSON request body. 
    /// For a partial refund, include an <code>amount</code> object in the JSON request body.
    /// </summary>
    public class CapturesRefundRequest : PayPalRequest<RefundMessage>
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

            ContentType = "application/json";
        }

        public CapturesRefundRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public CapturesRefundRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public CapturesRefundRequest WithBody(RefundMessage refundRequest)
        {
            Body = refundRequest;
            return this;
        }
    }
}
