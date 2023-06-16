using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    public static class IPaymentProviderExtensions
    {
        /// <summary>
        /// Checks whether payment method is enabled.
        /// </summary>
        /// <param name="provider">Payment provider.</param>
        /// <param name="paymentSettings">Payment settings.</param>
        /// <returns><c>True</c> if payment method is active, otherwise <c>false</c>.</returns>
        public static bool IsPaymentProviderEnabled(this Provider<IPaymentMethod> provider, PaymentSettings paymentSettings)
        {
            Guard.NotNull(provider);
            Guard.NotNull(paymentSettings);

            if (paymentSettings.ActivePaymentMethodSystemNames.IsNullOrEmpty())
            {
                return false;
            }

            return paymentSettings.ActivePaymentMethodSystemNames.Contains(provider.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }
    }
}