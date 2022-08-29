namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a webhook.
    /// </summary>
    public class CreateWebhookRequest : PayPalRequest<Webhook>
    {
        public CreateWebhookRequest()
            : base("/v1/notifications/webhooks", HttpMethod.Post)
        {
            ContentType = "application/json";
        }

        public CreateWebhookRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public CreateWebhookRequest WithBody(Webhook webhook)
        {
            Body = webhook;
            return this;
        }
    }
}
