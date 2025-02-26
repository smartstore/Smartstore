namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Suspends a subscription.
    /// </summary>
    public class SuspendSubscriptionRequest : PayPalRequest2<SuspendSubscriptionRequest, SubscriptionStateChangeMessage>
    {
        public SuspendSubscriptionRequest(string subscriptionId)
            : base("/v1/billing/subscriptions/{0}/suspend", HttpMethod.Post)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(subscriptionId));
            }
            catch (IOException)
            {
            }
        }
    }
}
