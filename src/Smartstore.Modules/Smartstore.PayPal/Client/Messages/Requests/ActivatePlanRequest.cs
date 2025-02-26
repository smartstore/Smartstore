namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Activates a PayPal plan.
    /// </summary>
    public class ActivatePlanRequest : PayPalRequest
    {
        public ActivatePlanRequest(string planId)
            : base("/v1/billing/plans/{0}/activate", HttpMethod.Post)
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
