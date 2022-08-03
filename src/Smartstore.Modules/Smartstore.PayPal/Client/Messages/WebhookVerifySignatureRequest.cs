namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Verifies a webhook request by checking its signature against the API.
    /// </summary>
    public class WebhookVerifySignatureRequest<T> : PayPalRequest<object>
    {
        public WebhookVerifySignatureRequest()
            : base("/v1/notifications/verify-webhook-signature?", HttpMethod.Post)
        {
            ContentType = "application/json";
        }

        public WebhookVerifySignatureRequest<T> WithBody(VerifyWebhookSignature<T> verifyWebhookSignature)
        {
            Body = verifyWebhookSignature;
            return this;
        }
    }
}
