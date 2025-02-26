namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Adds a tracking number to a PayPal order.
    /// </summary>
    public class CreatePlanRequest : PayPalRequest2<CreatePlanRequest, Plan>
    {
        public CreatePlanRequest()
            : base("/v1/billing/plans", HttpMethod.Post)
        {
        }
    }
}