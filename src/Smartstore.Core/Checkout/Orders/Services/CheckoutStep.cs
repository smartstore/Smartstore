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

        /// <summary>
        /// Gets the action result to the associated action method of this checkout step.
        /// </summary>
        public RedirectToActionResult? GetActionResult(CheckoutContext context)
        {
            Guard.NotNull(context);

            var md = Handler.Metadata;

            if (context.IsCurrentRoute(HttpMethods.Get, md.Controller, md.DefaultAction, md.Area))
            {
                // Avoid infinite redirection loop.
                return null;
            }

            return new RedirectToActionResult(md.DefaultAction, md.Controller, md.Area.HasValue() ? new { area = md.Area } : null);
        }

        /// <summary>
        /// Gets the URL to the associated action method of this checkout step.
        /// </summary>
        public string? GetUrl(CheckoutContext context)
        {
            Guard.NotNull(context);

            var md = Handler.Metadata;
            return context.UrlHelper.Action(md.DefaultAction, md.Controller, md.Area.HasValue() ? new { area = md.Area } : null);
        }
    }
}
