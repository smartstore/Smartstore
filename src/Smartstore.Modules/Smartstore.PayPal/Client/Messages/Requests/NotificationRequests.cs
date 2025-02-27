namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a webhook.
    /// </summary>
    public class CreateWebhookRequest : PayPalRequest<CreateWebhookRequest, Webhook>
    {
        public CreateWebhookRequest()
            : base("/v1/notifications/webhooks", HttpMethod.Post)
        {
        }
    }

    /// <summary>
    /// Lists webhooks for an app.
    /// </summary>
    public class ListWebhooksRequest : PayPalRequest<Webhooks>
    {
        public ListWebhooksRequest()
            : base("/v1/notifications/webhooks", HttpMethod.Get)
        {
        }
    }

    /// <summary>
    /// Verifies a webhook request by checking its signature against the API.
    /// </summary>
    public class VerifyWebhookSignatureRequest<T> : PayPalRequest<VerifyWebhookSignatureRequest<T>, VerifyWebhookSignature<T>>
        where T : class
    {
        public VerifyWebhookSignatureRequest()
            : base("/v1/notifications/verify-webhook-signature?", HttpMethod.Post)
        {
        }
    }
}
