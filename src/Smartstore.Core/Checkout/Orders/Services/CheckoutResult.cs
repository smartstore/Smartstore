#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutResult
    {
        public CheckoutResult(bool success, CheckoutError[]? errors = null, bool skipPage = false)
        {
            Success = success;
            Errors = errors ?? [];
            SkipPage = skipPage;
        }

        public CheckoutResult(CheckoutError[] errors, string? viewPath = null, bool success = false)
            : this(success, errors)
        {
            ViewPath = viewPath;
        }

        public CheckoutResult(IActionResult actionResult, string? viewPath = null)
            : this(false)
        {
            ActionResult = Guard.NotNull(actionResult);
            ViewPath = viewPath;
        }

        /// <summary>
        /// Gets a value indicating whether the processing was successful.
        /// If <c>true</c>, the next handler is called. Otherwise the customer is redirected to <see cref="ActionResult"/> or 
        /// the page belonging to current checkout step.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets a list of errors. Typically these are added to the model state by the caller
        /// to display them on current checkout page.
        /// </summary>
        public CheckoutError[] Errors { get; }

        /// <summary>
        /// Gets a value indicating whether the current checkout page should be skipped.
        /// Only applicable if <see cref="ICheckoutWorkflow.ProcessAsync"/> was called.
        /// </summary>
        public bool SkipPage { get; }

        /// <summary>
        /// Gets an <see cref="IActionResult"/> where the customer is to be redirected.
        /// If <c>null</c>, then the <see cref="ICheckoutWorkflow"/> implementation decides whether and where to redirect to.
        /// </summary>
        public IActionResult? ActionResult { get; set; }

        /// <summary>
        /// Gets the view path of the current checkout step.
        /// Always <c>null</c> for standard checkout (default).
        /// </summary>
        public string? ViewPath { get; set; }
    }

    public class CheckoutError(string propertyName, string errorMessage)
    {
        public string PropertyName { get; } = propertyName.EmptyNull();
        public string ErrorMessage { get; } = Guard.NotNull<string>(errorMessage);
    }
}
