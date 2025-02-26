namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Deactivates a PayPal plan.
    /// </summary>
    public class DeactivatePlanRequest : PayPalRequest
    {
        public DeactivatePlanRequest(string planId)
            : base("/v1/billing/plans/{0}/deactivate", HttpMethod.Post)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(planId));
            }
            catch (IOException)
            {
            }
        }
    }
}
