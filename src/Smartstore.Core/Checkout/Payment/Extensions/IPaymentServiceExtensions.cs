using System.Runtime.CompilerServices;

namespace Smartstore.Core.Checkout.Payment
{
    public static partial class IPaymentServiceExtensions
    {
        /// <summary>
        /// Gets a value indicating whether void is supported by payment method.
        /// </summary>
        /// <param name="systemName">Payment provider system name.</param>
        /// <returns>A value indicating whether void is supported.</returns>
        public static async Task<bool> SupportVoidAsync(this IPaymentService paymentService, string systemName)
        {
            Guard.NotNull(paymentService);

            var paymentMethod = await paymentService.LoadPaymentProviderBySystemNameAsync(systemName);

            return paymentMethod?.Value?.SupportVoid ?? false;
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported by payment method.
        /// </summary>
        /// <param name="systemName">Payment provider system name.</param>
        /// <returns>A value indicating whether refund is supported.</returns>
        public static async Task<bool> SupportRefundAsync(this IPaymentService paymentService, string systemName)
        {
            Guard.NotNull(paymentService);

            var paymentMethod = await paymentService.LoadPaymentProviderBySystemNameAsync(systemName);

            return paymentMethod?.Value?.SupportRefund ?? false;
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported by payment method.
        /// </summary>
        /// <param name="systemName">Payment provider system name.</param>
        /// <returns>A value indicating whether partial refund is supported.</returns>
        public static async Task<bool> SupportPartiallyRefundAsync(this IPaymentService paymentService, string systemName)
        {
            Guard.NotNull(paymentService);

            var paymentMethod = await paymentService.LoadPaymentProviderBySystemNameAsync(systemName);

            return paymentMethod?.Value?.SupportPartiallyRefund ?? false;
        }

        /// <summary>
        /// Gets a value indicating whether the payment method supports capture.
        /// </summary>
        /// <param name="systemName">Payment provider system name.</param>
        /// <returns>A value indicating whether capture is supported.</returns>
        public static async Task<bool> SupportCaptureAsync(this IPaymentService paymentService, string systemName)
        {
            Guard.NotNull(paymentService);

            var paymentMethod = await paymentService.LoadPaymentProviderBySystemNameAsync(systemName);

            return paymentMethod?.Value?.SupportCapture ?? false;
        }

        /// <summary>
        /// Gets a payment method type.
        /// </summary>
        /// <param name="systemName">Payment provider system name.</param>
        /// <returns>A payment method type.</returns>
        public static async Task<PaymentMethodType> GetPaymentProviderTypeAsync(this IPaymentService paymentService, string systemName)
        {
            Guard.NotNull(paymentService);

            var paymentMethod = await paymentService.LoadPaymentProviderBySystemNameAsync(systemName);

            return paymentMethod?.Value?.PaymentMethodType ?? PaymentMethodType.Unknown;
        }

        /// <summary>
        /// Gets a recurring payment type of payment method.
        /// </summary>
        /// <param name="systemName">Payment provider system name.</param>
        /// <returns>A recurring payment type of payment method.</returns>
        public static async Task<RecurringPaymentType> GetRecurringPaymentTypeAsync(this IPaymentService paymentService, string systemName)
        {
            Guard.NotNull(paymentService);

            var paymentMethod = await paymentService.LoadPaymentProviderBySystemNameAsync(systemName);

            return paymentMethod?.Value?.RecurringPaymentType ?? RecurringPaymentType.NotSupported;
        }
    }
}
