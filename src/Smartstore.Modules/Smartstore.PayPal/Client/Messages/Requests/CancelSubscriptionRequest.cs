namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    public class CancelSubscriptionRequest : PayPalRequest<object>
    {
        public CancelSubscriptionRequest(string subscriptionId)
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

        public CancelSubscriptionRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public CancelSubscriptionRequest WithBody(SubscriptionStateChangeMessage subscriptionStateChangeMessage)
        {
            Body = subscriptionStateChangeMessage;
            return this;
        }
    }
}
