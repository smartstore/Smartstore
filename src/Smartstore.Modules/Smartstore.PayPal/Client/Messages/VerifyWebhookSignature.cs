namespace Smartstore.PayPal.Client.Messages
{
    public class VerifyWebhookSignature<T>
    {
        public string AuthAlgo;
        public string CertUrl;
        public string TransmissionId;
        public string TransmissionSig;
        public string TransmissionTime;
        public WebhookEvent<WebhookResource> WebhookEvent;
        public string WebhookId;
    }
}
