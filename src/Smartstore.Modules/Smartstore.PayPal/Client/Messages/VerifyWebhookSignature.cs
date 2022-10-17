namespace Smartstore.PayPal.Client.Messages
{
    public class VerifyWebhookSignature<T>
    {
        [JsonProperty("auth_algo", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AuthAlgo;

        [JsonProperty("cert_url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CertUrl;

        [JsonProperty("transmission_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TransmissionId;

        [JsonProperty("transmission_sig", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TransmissionSig;

        [JsonProperty("transmission_time", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TransmissionTime;

        [JsonProperty("webhook_event", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public WebhookEvent<WebhookResource> WebhookEvent;

        [JsonProperty("webhook_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string WebhookId;
    }
}
