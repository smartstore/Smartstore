#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    public partial interface ICheckoutWorkflow
    {
        Task<IActionResult> StartAsync();
        Task<IActionResult> AdvanceAsync();
        Task<IActionResult> CompleteAsync();
    }

    //public partial class CheckoutWorkflowResult
    //{
    //    public CheckoutWorkflowResult(IActionResult result)
    //    {
    //        Result = Guard.NotNull(result);
    //    }

    //    public CheckoutWorkflowResult(string routeName, object? routeValues = null)
    //    {
    //        Result = new RedirectToRouteResult(routeName, routeValues);
    //    }

    //    public CheckoutWorkflowResult(string action, string controller, object? routeValues = null)
    //    {
    //        Result = new RedirectToActionResult(action, controller, routeValues);
    //    }

    //    public bool Success
    //        => Warnings.Count == 0;

    //    public IActionResult Result { get; }
    //    public IList<string> Warnings { get; } = new List<string>();
    //}
}
