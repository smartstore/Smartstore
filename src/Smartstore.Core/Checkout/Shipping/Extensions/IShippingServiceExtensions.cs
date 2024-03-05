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
        /// <param name="allowedShippingProviderSystemName">
        /// Filter by shipping rate computation provider system name.
        /// <c>null</c> to load shipping options of all shipping rate computation providers.
        /// </param>
        /// <param name="storeId">Store identifier.</param>
        /// <param name="matchRules">A value indicating whether shipping methods must match cart rules.</param>
        /// <returns>Shipping option response.</returns>
        public static Task<ShippingOptionResponse> GetShippingOptionsAsync(
            this IShippingService shippingService,
            ShoppingCart cart,
            Address shippingAddress,
            string allowedShippingProviderSystemName = null,
            int storeId = 0,
            bool matchRules = true)
        {
            Guard.NotNull(shippingService);
            Guard.NotNull(cart);

            var request = shippingService.CreateShippingOptionRequest(cart, shippingAddress, storeId, matchRules);

            return shippingService.GetShippingOptionsAsync(request, allowedShippingProviderSystemName);
        }
    }
}
