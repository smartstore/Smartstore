namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Activates a PayPal plan.
    /// </summary>
    public class ActivatePlanRequest(string planId) : PayPalRequest(FormatPath("/v1/billing/plans/{0}/activate", planId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Adds a tracking number to a PayPal order.
    /// </summary>
    public class CreatePlanRequest : PayPalRequest<CreatePlanRequest, Plan>
    {
        public CreatePlanRequest()
            : base("/v1/billing/plans", HttpMethod.Post)
        {
        }
    }

    /// <summary>
    /// Deactivates a PayPal plan.
    /// </summary>
    public class DeactivatePlanRequest(string planId) : PayPalRequest(FormatPath("/v1/billing/plans/{0}/deactivate", planId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Gets plan details.
    /// </summary>
    public class GetPlanDetailsRequest : PayPalRequest<Plan>
    {
        public GetPlanDetailsRequest(string planId)
            : base(FormatPath("/v1/billing/plans/{0}", planId), HttpMethod.Get)
        {
        }
    }
}
