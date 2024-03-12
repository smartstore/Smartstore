#nullable enable

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a checkout flow by processing checkout steps through checkout handlers.
    /// </summary>
    /// <remarks>
    /// Only applicable in the context of an HTTP request.
    /// </remarks>
    public partial interface ICheckoutWorkflow
    {
        /// <summary>
        /// Initializes and starts the checkout.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutResult.ActionResult"/> to the first, unfulfilled checkout page.
        /// </returns>
        Task<CheckoutResult> StartAsync(CheckoutContext context);

        /// <summary>
        /// Processes the current checkout step.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutResult.ActionResult"/> to the next/previous checkout page, if the current page should be skipped.
        /// Otherwise <see cref="CheckoutResult.ActionResult"/> is <c>null</c> (default).
        /// </returns>
        /// <remarks>
        /// Typically called in GET requests when the current checkout page is to be opened.
        /// </remarks>
        Task<CheckoutResult> ProcessAsync(CheckoutContext context);

        /// <summary>
        /// Advances in checkout.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutResult.ActionResult"/> to the next, unfulfilled checkout page.
        /// If <see cref="CheckoutResult.ActionResult"/> is <c>null</c> (not determinable), then the caller has to specify the next step.
        /// </returns>
        /// <remarks>
        /// Typically called in POST requests when the customer has made a selection in checkout.
        /// </remarks>
        Task<CheckoutResult> AdvanceAsync(CheckoutContext context);

        /// <summary>
        /// Completes the checkout and places a new order.
        /// </summary>
        /// <returns>
        /// <see cref="CheckoutResult.ActionResult"/> to the completed page, if operation succeeded.
        /// Otherwise result to redirect to an error related checkout page like payment method selection page.
        /// </returns>
        Task<CheckoutResult> CompleteAsync(CheckoutContext context);
    }
}
