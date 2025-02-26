namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Verifies a webhook request by checking its signature against the API.
    /// </summary>
    public class VerifyWebhookSignatureRequest<T> : PayPalRequest2<VerifyWebhookSignatureRequest<T>, VerifyWebhookSignature<T>> 
        where T : class
    {
        public VerifyWebhookSignatureRequest()
            : base("/v1/notifications/verify-webhook-signature?", HttpMethod.Post)
        {
        }
    }
}
