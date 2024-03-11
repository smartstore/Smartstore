#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutWorkflowResult
    {
        public CheckoutWorkflowResult(IActionResult? result, string? viewPath = null, CheckoutWorkflowError[]? errors = null)
        {
            ActionResult = result;
            ViewPath = viewPath;
            Errors = errors ?? [];
        }

        public CheckoutWorkflowError[] Errors { get; }

        /// <summary>
        /// Gets an <see cref="IActionResult"/> where the customer is to be redirected.
        /// See the methods of <see cref="ICheckoutWorkflow"/> for more details.
        /// If <c>null</c> (not determinable), then the caller has to specify the next step.
        /// </summary>
        public IActionResult? ActionResult { get; }

        /// <summary>
        /// Gets the view path of the current checkout step.
        /// Always <c>null</c> for standard checkout (default).
        /// </summary>
        public string? ViewPath { get; init; }
    }

    public class CheckoutWorkflowError(string propertyName, string errorMessage)
    {
        public string PropertyName { get; } = propertyName.EmptyNull();
        public string ErrorMessage { get; } = Guard.NotNull<string>(errorMessage);
    }
}
