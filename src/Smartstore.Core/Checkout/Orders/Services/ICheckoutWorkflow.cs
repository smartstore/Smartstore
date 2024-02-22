#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a checkout flow processing, which checks the requirements of the individual checkout steps.
    /// </summary>
    /// <remarks>
    /// Only applicable in the context of a HTTP request.
    /// </remarks>
    public partial interface ICheckoutWorkflow
    {
        /// <summary>
        /// Initializes and starts the checkout.
        /// </summary>
        /// <returns><see cref="CheckoutWorkflowResult.Result"/> of the first checkout page.</returns>
        Task<CheckoutWorkflowResult> StartAsync();

        /// <summary>
        /// Checks the requirement for the current checkout page.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutWorkflowResult.Result"/> to an adjacent checkout page, if the current page should be skipped.
        /// Otherwise <see cref="CheckoutWorkflowResult.Result"/> is <c>null</c> (default).
        /// </returns>
        Task<CheckoutWorkflowResult> StayAsync();

        /// <summary>
        /// Checks whether all checkout requirements are fulfilled and advances in checkout, if no error occurred.
        /// </summary>
        /// <param name="model">
        /// An optional model (usually of a simple type) representing the data to fulfill the requirement(s) of the current checkout page.
        /// </param>
        /// <returns>
        /// <see cref="CheckoutWorkflowResult.Result"/> to the next checkout page.
        /// If <see cref="CheckoutWorkflowResult.Result"/> is <c>null</c> (not determinable), then the caller has to specify the next step.
        /// </returns>
        Task<CheckoutWorkflowResult> AdvanceAsync(object? model = null);

        /// <summary>
        /// Completes the checkout and places a new order.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutWorkflowResult.Result"/> to the confirmation page, if operation succeeded.
        /// Otherwise it redirects to an error related checkout page like payment method selection page.
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
