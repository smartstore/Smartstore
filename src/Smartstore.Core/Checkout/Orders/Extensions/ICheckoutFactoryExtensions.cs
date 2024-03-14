using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    public static partial class ICheckoutFactoryExtensions
    {
        /// <summary>
        /// Get the checkout step for the current request.
        /// </summary>
        public static CheckoutStep GetCheckoutStep(this ICheckoutFactory factory, CheckoutContext context)
        {
            Guard.NotNull(context);
            Guard.NotNull(context.RouteValues);

            return factory.GetCheckoutStep(
                context.RouteValues.GetActionName(),
                context.RouteValues.GetControllerName(),
                context.RouteValues.GetAreaName());
        }
    }
}
