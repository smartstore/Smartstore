namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets plan details.
    /// </summary>
    public class GetPlanDetailsRequest : PayPalRequest<Plan>
    {
        public GetPlanDetailsRequest(string planId)
            : base("/v1/billing/plans/{0}", HttpMethod.Get)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(planId));
            }
            catch (IOException)
            {
            }

            ContentType = "application/json";
        }
    }
}