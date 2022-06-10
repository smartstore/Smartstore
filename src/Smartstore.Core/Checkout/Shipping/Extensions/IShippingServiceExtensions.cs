using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Shipping
{
    public static partial class IShippingServiceExtensions
    {
        /// <summary>
        /// Gets shipping options for a shopping cart.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="shippingAddress">Shipping Address.</param>
        /// <param name="allowedShippingRateComputationMethodSystemName">
        /// Filter by shipping rate computation method system name.
        /// <c>null</c> to load shipping options of all shipping rate computation methods.
        /// </param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Shipping option response.</returns>
        public static Task<ShippingOptionResponse> GetShippingOptionsAsync(
            this IShippingService shippingService,
            ShoppingCart cart,
            Address shippingAddress,
            string allowedShippingRateComputationMethodSystemName = null,
            int storeId = 0)
        {
            Guard.NotNull(shippingService, nameof(shippingService));
            Guard.NotNull(cart, nameof(cart));

            var request = shippingService.CreateShippingOptionRequest(cart, shippingAddress, storeId);

            return shippingService.GetShippingOptionsAsync(request, allowedShippingRateComputationMethodSystemName);
        }
    }
}
