namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Activates a subscription.
    /// </summary>
    public class ActivateSubscriptionRequest(string subscriptionId) : PayPalRequest<ActivateSubscriptionRequest, object>(FormatPath("/v1/billing/subscriptions/{0}/activate", subscriptionId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    public class CancelSubscriptionRequest(string subscriptionId) : PayPalRequest<CancelSubscriptionRequest, object>(FormatPath("/v1/billing/subscriptions/{0}/activate", subscriptionId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Creates a subscription.
    /// </summary>
    public class CreateSubscriptionRequest : PayPalRequest<CreateSubscriptionRequest, Subscription>
    {
        public CreateSubscriptionRequest()
            : base("/v1/billing/subscriptions", HttpMethod.Post)
        {
        }
    }

    /// <summary>
    /// Suspends a subscription.
    /// </summary>
    public class SuspendSubscriptionRequest : PayPalRequest<SuspendSubscriptionRequest, SubscriptionStateChangeMessage>
    {
        public SuspendSubscriptionRequest(string subscriptionId)
            : base(FormatPath("/v1/billing/subscriptions/{0}/suspend", subscriptionId), HttpMethod.Post)
        {
        }
    }
}
