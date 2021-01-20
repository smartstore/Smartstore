using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Engine.Modularity;

namespace Smartstore
{
    public static class ShippingExtentions
    {
        /// <summary>
        /// Checks whether shipping rate computation method is active
        /// </summary>
        /// <returns>
        /// <c>True</c> if is active, otherwise <c>false</c>
        /// </returns>
        public static bool IsShippingRateComputationMethodActive(this Provider<IShippingRateComputationMethod> method, ShippingSettings settings)
        {
            Guard.NotNull(method, nameof(method));
            Guard.NotNull(settings, nameof(settings));

            if (settings.ActiveShippingRateComputationMethodSystemNames.IsNullOrEmpty() || !method.Value.IsActive)
                return false;

            return settings.ActiveShippingRateComputationMethodSystemNames.Contains(method.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets shipping option response.
        /// </summary>
        /// <param name="cart">List of <see cref="OrganizedShoppingCartItem"/>.</param>
        /// <param name="shippingAddress">Shipping Address.</param>
        /// <param name="allowedShippingRateComputationMethodSystemName">Allowed shipping rate computation method system name.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns><see cref="ShippingOptionRequest"/></returns>
        public static ShippingOptionResponse GetShippingOptions(
            this IShippingService shippingService,
            IList<OrganizedShoppingCartItem> cart,
            Address shippingAddress,
            string allowedShippingRateComputationMethodSystemName = "",
            int storeId = 0)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(shippingService, nameof(shippingService));
            Guard.NotNull(shippingAddress, nameof(shippingAddress));

            return shippingService.GetShippingOptions(
                shippingService.CreateShippingOptionRequest(cart, shippingAddress, storeId), 
                allowedShippingRateComputationMethodSystemName);
        }
    }
}