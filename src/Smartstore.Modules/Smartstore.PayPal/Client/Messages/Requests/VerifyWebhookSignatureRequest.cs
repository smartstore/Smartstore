namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Verifies a webhook request by checking its signature against the API.
    /// </summary>
    public class VerifyWebhookSignatureRequest<T> : PayPalRequest<object> where T : class
    {
        public VerifyWebhookSignatureRequest()
            : base("/v1/notifications/verify-webhook-signature?", HttpMethod.Post)
        {
            ContentType = "application/json";
        }

        public VerifyWebhookSignatureRequest<T> WithBody(VerifyWebhookSignature<T> verifyWebhookSignature)
        {
            Body = verifyWebhookSignature;
            return this;
        }
    }
}
