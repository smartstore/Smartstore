using Microsoft.AspNetCore.Builder;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Bootstrapping
{
    public static class CheckoutBootstrappingExtensions
    {
        /// <summary>
        /// Adds a <see cref="CheckoutState"/> middleware to the application. Ensures that the
        /// <see cref="CheckoutState"/> instance is automatically saved right before session
        /// data is committed (but only if state was loaded and changed during the request).
        /// Should be registered right after the session middleware.
        /// </summary>
        public static IApplicationBuilder UseCheckoutState(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var accessor = context.RequestServices.GetService<ICheckoutStateAccessor>();

                try
                {
                    await next();
                }
                finally
                {
                    if (accessor != null && accessor.IsStateLoaded && accessor.HasStateChanged)
                    {
                        accessor.Save();
                    }
                }
            });
        }
    }
}
