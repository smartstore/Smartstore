namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Creates a subscription.
    /// </summary>
    public class CreateSubscriptionRequest : PayPalRequest2<CreateSubscriptionRequest, Subscription>
    {
        public CreateSubscriptionRequest()
            : base("/v1/billing/subscriptions", HttpMethod.Post)
        {
        }
    }
}