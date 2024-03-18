#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    public static partial class ICheckoutFactoryExtensions
    {
        /// <summary>
        /// Get the checkout step for the current request.
        /// </summary>
        public static CheckoutStep? GetCheckoutStep(this ICheckoutFactory factory, CheckoutContext context)
        {
            Guard.NotNull(context);
            Guard.NotNull(context.RouteValues);

            return factory.GetCheckoutStep(
                context.RouteValues.GetActionName(),
                context.RouteValues.GetControllerName(),
                context.RouteValues.GetAreaName());
        }

        /// <summary>
        /// Gets the URL of the next/previous step in checkout.
        /// If this cannot be determined, the URL to the confirmation page is returned if <paramref name="next"/> is <c>true</c>,
        /// otherwise the URL to the shopping cart page.
        /// </summary>
        /// <param name="next"><c>true</c> to get the next, <c>false</c> to get the previous checkout step.</param>
        public static string? GetNextCheckoutStepUrl(this ICheckoutFactory factory, CheckoutContext context, bool next)
        {
            var step = factory.GetCheckoutStep(context);
            var nextStep = factory.GetNextCheckoutStep(step, next);

            var url = nextStep?.GetUrl(context);
            url ??= (next ? context.UrlHelper.Action(CheckoutActionNames.Confirm, "Checkout", null) : context.UrlHelper.RouteUrl("ShoppingCart"));

            return url;
        }
    }
}
