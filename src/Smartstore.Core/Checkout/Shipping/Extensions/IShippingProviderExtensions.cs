using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{
    public static partial class IShippingProviderExtensions
    {
        /// <summary>
        /// Checks whether a shipping rate computation method is active.
        /// </summary>
        /// <returns><c>True</c> if is active, otherwise <c>false</c>.</returns>
        public static bool IsShippingProviderEnabled(this Provider<IShippingRateComputationMethod> provider, ShippingSettings settings)
        {
            Guard.NotNull(provider);
            Guard.NotNull(settings);

            if (settings.ActiveShippingRateComputationMethodSystemNames.IsNullOrEmpty())
            {
                return false;
            }

            return settings.ActiveShippingRateComputationMethodSystemNames.Contains(provider.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }
    }
}