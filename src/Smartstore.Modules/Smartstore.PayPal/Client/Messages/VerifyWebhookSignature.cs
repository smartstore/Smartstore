namespace Smartstore.PayPal.Client.Messages
{
    public class VerifyWebhookSignature<T> where T : class
    {
        public string AuthAlgo;
        public string CertUrl;
        public string TransmissionId;
        public string TransmissionSig;
        public string TransmissionTime;
        public WebhookEvent<T> WebhookEvent;
        public string WebhookId;
    }
}
