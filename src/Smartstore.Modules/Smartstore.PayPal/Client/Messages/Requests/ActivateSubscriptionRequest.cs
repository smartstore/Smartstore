namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Activates a subscription.
    /// </summary>
    public class ActivateSubscriptionRequest : PayPalRequest<object>
    {
        public ActivateSubscriptionRequest(string subscriptionId)
            : base("/v1/billing/subscriptions/{0}/activate", HttpMethod.Post)
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

        public ActivateSubscriptionRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public ActivateSubscriptionRequest WithBody(SubscriptionStateChangeMessage subscriptionStateChangeMessage)
        {
            Body = subscriptionStateChangeMessage;
            return this;
        }
    }
}
