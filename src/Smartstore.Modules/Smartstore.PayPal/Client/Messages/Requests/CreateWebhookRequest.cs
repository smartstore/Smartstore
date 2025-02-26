namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a webhook.
    /// </summary>
    public class CreateWebhookRequest : PayPalRequest2<CreateWebhookRequest, Webhook>
    {
        public CreateWebhookRequest()
            : base("/v1/notifications/webhooks", HttpMethod.Post)
        {
        }
    }
}
