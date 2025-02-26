namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    public class CancelSubscriptionRequest : PayPalRequest2<CancelSubscriptionRequest, object>
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
        }
    }
}
