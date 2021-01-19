using System;
using System.Linq;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Engine.Modularity;

namespace Smartstore
{
    public static class ShippingExtentions
    {
        /// <summary>
        /// Checks whether shipping rate computation method is active
        /// </summary>
        /// <returns>
        /// <c>True</c> if is active, <c>false</c> if it is not active
        /// </returns>
        public static bool IsShippingRateComputationMethodActive(this Provider<IShippingRateComputationMethod> method, ShippingSettings settings)
        {
            Guard.NotNull(method, nameof(method));
            Guard.NotNull(settings, nameof(settings));

            if (settings.ActiveShippingRateComputationMethodSystemNames.IsNullOrEmpty() || !method.Value.IsActive)
                return false;

            return settings.ActiveShippingRateComputationMethodSystemNames.Contains(method.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }
    }
}