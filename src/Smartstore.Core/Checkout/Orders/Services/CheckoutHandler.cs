#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Orders
{
    public interface ICheckoutHandler
    {
        /// <summary>
        /// Processes a checkout step.
        /// </summary>
        Task<CheckoutHandlerResult> ProcessAsync(CheckoutContext context);
    }

    public sealed class CheckoutHandler(Lazy<ICheckoutHandler, CheckoutHandlerMetadata> lazy)
    {
        private readonly Lazy<ICheckoutHandler, CheckoutHandlerMetadata> _lazy = lazy;

        public ICheckoutHandler Value => _lazy.Value;
        public CheckoutHandlerMetadata Metadata => _lazy.Metadata;

        public bool IsValueCreated => _lazy.IsValueCreated;

        public Lazy<ICheckoutHandler, CheckoutHandlerMetadata> ToLazy() => _lazy;

        /// <summary>
        /// Gets the action result to the associated action method of the handler.
        /// </summary>
        public RedirectToActionResult? GetActionResult(CheckoutContext context)
        {
            if (context.IsCurrentRoute(HttpMethods.Get, Metadata.Controller, Metadata.Actions[0], Metadata.Area))
            {
                // Avoid infinite redirection loop.
                return null;
            }

            return new RedirectToActionResult(Metadata.Actions[0], Metadata.Controller, Metadata.Area.HasValue() ? new { area = Metadata.Area } : null);
        }

        public override string ToString()
            => _lazy.Metadata.ToString();
    }
}
