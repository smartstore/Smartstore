#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a checkout flow to be processed, which checks the requirements of the individual checkout steps.
    /// </summary>
    /// <remarks>
    /// Only applicable in the context of a HTTP request.
    /// </remarks>
    public partial interface ICheckoutWorkflow
    {
        /// <summary>
        /// Initializes and starts the checkout.
        /// </summary>
        /// <returns>The first checkout page.</returns>
        Task<CheckoutWorkflowResult> StartAsync();

        Task<CheckoutWorkflowResult> StayAsync(object? model = null);

        /// <summary>
        /// Checks whether all checkout requirements are fulfilled and advances in checkout.
        /// </summary>
        /// <param name="model">
        /// An optional model (usually of a simple type) representing the data to fulfill the requirement(s) of the current checkout page.
        /// </param>
        /// <returns>
        /// The checkout page to be redirected to if the associated requirement is not fulfilled and no model state errors exist.
        /// A redirect result to the confirmation page if all requirements are fulfilled.
        /// <c>null</c> if the requirement of the current page is not fulfilled and the view of the current page should be displayed.
        /// </returns>
        Task<CheckoutWorkflowResult> AdvanceAsync(object? model = null);

        /// <summary>
        /// Completes the checkout and places a new order.
        /// </summary>
        /// <returns>
        /// Depending of the result of the processing, a redirect result to a checkout, confirm or completed page.
        /// </returns>
        Task<CheckoutWorkflowResult> CompleteAsync();
    }

    public partial class CheckoutWorkflowResult(IActionResult? result, CheckoutWorkflowError[]? errors = null)
    {
        public IActionResult? Result { get; } = result;
        public CheckoutWorkflowError[] Errors { get; } = errors ?? [];
    }

    public class CheckoutWorkflowError(string propertyName, string errorMessage)
    {
        public string PropertyName { get; } = propertyName.EmptyNull();
        public string ErrorMessage { get; } = Guard.NotNull<string>(errorMessage);
    }
}
