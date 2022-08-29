namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Lists webhooks for an app.
    /// </summary>
    public class ListWebhooksRequest : PayPalRequest<Webhooks>
    {
        public ListWebhooksRequest()
            : base("/v1/notifications/webhooks", HttpMethod.Get)
        {
            ContentType = "application/json";
        }
    }
}
