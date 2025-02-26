namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Activates a subscription.
    /// </summary>
    public class ActivateSubscriptionRequest : PayPalRequest2<ActivateSubscriptionRequest, object>
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
        }
    }
}
