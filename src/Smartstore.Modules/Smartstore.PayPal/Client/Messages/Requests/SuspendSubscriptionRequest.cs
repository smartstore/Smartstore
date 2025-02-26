namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Suspends a subscription.
    /// </summary>
    public class SuspendSubscriptionRequest : PayPalRequest<object>
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

            ContentType = "application/json";
        }

        public SuspendSubscriptionRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public SuspendSubscriptionRequest WithBody(SubscriptionStateChangeMessage subscriptionStateChangeMessage)
        {
            Body = subscriptionStateChangeMessage;
            return this;
        }
    }
}
