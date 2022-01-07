using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    public static class PaymentExtentions
    {
        /// <summary>
        /// Checks whether payment method is active.
        /// </summary>
        /// <param name="paymentMethod">Payment method.</param>
        /// <param name="paymentSettings">Payment settings.</param>
        /// <returns><c>True</c> if payment method is active, otherwise <c>false</c>.</returns>
        public static bool IsPaymentMethodActive(this Provider<IPaymentMethod> paymentMethod, PaymentSettings paymentSettings)
        {
            Guard.NotNull(paymentMethod, nameof(paymentMethod));
            Guard.NotNull(paymentSettings, nameof(paymentSettings));

            if (paymentSettings.ActivePaymentMethodSystemNames == null || !paymentMethod.Value.IsActive)
            {
                return false;
            }

            return paymentSettings.ActivePaymentMethodSystemNames.Contains(paymentMethod.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }
    }
}