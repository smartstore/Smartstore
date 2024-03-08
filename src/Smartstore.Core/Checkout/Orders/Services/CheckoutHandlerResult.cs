#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutHandlerResult
    {
        public CheckoutHandlerResult(bool success, CheckoutWorkflowError[]? errors = null, bool skipPage = false)
        {
            Success = success;
            Errors = errors ?? [];
            SkipPage = skipPage;
        }

        public CheckoutHandlerResult(IActionResult actionResult, bool success = false)
            : this(success)
        {
            ActionResult = Guard.NotNull(actionResult);
        }

        /// <summary>
        /// Gets a value indicating whether the processing of the handler was successful.
        /// If <c>true</c>, the next handler is called. Otherwise the customer is redirected to <see cref="ActionResult"/> or 
        /// the page belonging to the current handler.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets a list of errors. Typically these are added to the model state by the caller
        /// to display them on current checkout page.
        /// </summary>
        public CheckoutWorkflowError[] Errors { get; }

        /// <summary>
        /// Gets a value indicating whether the current checkout page should be skipped
        /// if <see cref="ICheckoutWorkflow.ProcessAsync"/> is called.
        /// </summary>
        public bool SkipPage { get; set; }

        /// <summary>
        /// Gets an <see cref="IActionResult"/> where the customer is to be redirected.
        /// If <c>null</c>, then the <see cref="ICheckoutWorkflow"/> implementation decides whether and where to redirect to.
        /// </summary>
        public IActionResult? ActionResult { get; }
    }
}
