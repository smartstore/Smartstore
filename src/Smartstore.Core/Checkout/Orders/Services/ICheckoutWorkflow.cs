#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a checkout flow by processing checkout steps through checkout handlers.
    /// </summary>
    /// <remarks>
    /// Only applicable in the context of a HTTP request.
    /// </remarks>
    public partial interface ICheckoutWorkflow
    {
        /// <summary>
        /// Initializes and starts the checkout.
        /// </summary>
        /// <returns><see cref="CheckoutWorkflowResult.ActionResult"/> of the first checkout page.</returns>
        Task<CheckoutWorkflowResult> StartAsync();

        /// <summary>
        /// Processes the current checkout step.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutWorkflowResult.ActionResult"/> to an adjacent checkout page, if the current page should be skipped.
        /// Otherwise <see cref="CheckoutWorkflowResult.ActionResult"/> is <c>null</c> (default).
        /// </returns>
        Task<CheckoutWorkflowResult> ProcessAsync();

        /// <summary>
        /// Advances in checkout.
        /// </summary>
        /// <param name="model">
        /// An optional model (usually of a simple type) representing a user selection (e.g. address ID, shipping method ID or payment method system name).
        /// </param>
        /// <returns>
        /// <see cref="CheckoutWorkflowResult.ActionResult"/> to the next checkout page.
        /// If <see cref="CheckoutWorkflowResult.ActionResult"/> is <c>null</c> (not determinable), then the caller has to specify the next step.
        /// </returns>
        Task<CheckoutWorkflowResult> AdvanceAsync(object? model = null);

        /// <summary>
        /// Completes the checkout and places a new order.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutWorkflowResult.ActionResult"/> to the confirmation page, if operation succeeded.
        /// Otherwise it redirects to an error related checkout page like payment method selection page.
        /// </returns>
        Task<CheckoutWorkflowResult> CompleteAsync();
    }

    public partial class CheckoutWorkflowResult(IActionResult? result, CheckoutWorkflowError[]? errors = null)
    {
        public IActionResult? ActionResult { get; } = result;
        public CheckoutWorkflowError[] Errors { get; } = errors ?? [];
    }

    public class CheckoutWorkflowError(string propertyName, string errorMessage)
    {
        public string PropertyName { get; } = propertyName.EmptyNull();
        public string ErrorMessage { get; } = Guard.NotNull<string>(errorMessage);
    }
}
