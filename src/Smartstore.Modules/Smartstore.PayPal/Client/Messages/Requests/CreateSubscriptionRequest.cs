namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a subscription.
    /// </summary>
    public class CreateSubscriptionRequest : PayPalRequest<Subscription>
    {
        public CreateSubscriptionRequest()
            : base("/v1/billing/subscriptions", HttpMethod.Post)
        {
            ContentType = "application/json";
        }

        public CreateSubscriptionRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public CreateSubscriptionRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public CreateSubscriptionRequest WithBody(Subscription subscription)
        {
            Body = subscription;
            return this;
        }
    }
}