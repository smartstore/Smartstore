namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Adds a tracking number to a PayPal order.
    /// </summary>
    public class CreatePlanRequest : PayPalRequest<Plan>
    {
        public CreatePlanRequest()
            : base("/v1/billing/plans", HttpMethod.Post)
        {
            ContentType = "application/json";
        }

        public CreatePlanRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public CreatePlanRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public CreatePlanRequest WithBody(Plan plan)
        {
            Body = plan;
            return this;
        }
    }
}