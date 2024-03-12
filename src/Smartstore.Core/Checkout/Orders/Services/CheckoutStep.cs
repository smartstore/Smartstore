#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutStep(CheckoutHandler handler, string? viewPath = null)
    {
        /// <summary>
        /// Gets the handler to process the checkout step.
        /// </summary>
        public CheckoutHandler Handler { get; } = Guard.NotNull(handler);

        /// <summary>
        /// Gets the path of the view associated with the <see cref="Handler"/>.
        /// Always <c>null</c> for standard checkout (default).
        /// </summary>
        public string? ViewPath { get; } = viewPath;

        /// <inheritdoc cref="ICheckoutHandler.ProcessAsync(CheckoutContext)" />
        public async Task<CheckoutResult> ProcessAsync(CheckoutContext context)
        {
            var result = await Handler.Value.ProcessAsync(context);
            result.ViewPath = ViewPath;

            if (!result.Success)
            {
                // Redirect to the page associated with this step.
                result.ActionResult ??= GetActionResult(context);
            }

            return result;
        }

        /// <summary>
        /// Gets the action result to the associated action method of this checkout step.
        /// </summary>
        public RedirectToActionResult? GetActionResult(CheckoutContext context)
        {
            Guard.NotNull(context);

            var md = Handler.Metadata;

            if (context.IsCurrentRoute(HttpMethods.Get, md.Controller, md.Actions[0], md.Area))
            {
                // Avoid infinite redirection loop.
                return null;
            }

            return new RedirectToActionResult(md.Actions[0], md.Controller, md.Area.HasValue() ? new { area = md.Area } : null);
        }
    }
}
