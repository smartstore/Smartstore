using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{
    public static partial class IShippingRateComputationMethodExtensions
    {
        /// <summary>
        /// Checks whether a shipping rate computation method is active.
        /// </summary>
        /// <returns><c>True</c> if is active, otherwise <c>false</c>.</returns>
        public static bool IsShippingRateComputationMethodActive(this Provider<IShippingRateComputationMethod> method, ShippingSettings settings)
        {
            Guard.NotNull(method, nameof(method));
            Guard.NotNull(settings, nameof(settings));

            if (settings.ActiveShippingRateComputationMethodSystemNames.IsNullOrEmpty() || !method.Value.IsActive)
            {
                return false;
            }

            return settings.ActiveShippingRateComputationMethodSystemNames.Contains(method.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }
    }
}