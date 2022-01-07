using System.Runtime.CompilerServices;

namespace Smartstore.Core.Checkout.Payment
{
    public static partial class IPaymentServiceExtensions
    {
        /// <summary>
        /// Gets a value indicating whether void is supported by payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether void is supported.</returns>
        public static async Task<bool> SupportVoidAsync(this IPaymentService paymentService, string paymentMethodSystemName)
        {
            Guard.NotNull(paymentService, nameof(paymentService));

            var paymentMethod = await paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);

            return paymentMethod?.Value?.SupportVoid ?? false;
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported by payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether refund is supported.</returns>
        public static async Task<bool> SupportRefundAsync(this IPaymentService paymentService, string paymentMethodSystemName)
        {
            Guard.NotNull(paymentService, nameof(paymentService));

            var paymentMethod = await paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);

            return paymentMethod?.Value?.SupportRefund ?? false;
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported by payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether partial refund is supported.</returns>
        public static async Task<bool> SupportPartiallyRefundAsync(this IPaymentService paymentService, string paymentMethodSystemName)
        {
            Guard.NotNull(paymentService, nameof(paymentService));

            var paymentMethod = await paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);

            return paymentMethod?.Value?.SupportPartiallyRefund ?? false;
        }

        /// <summary>
        /// Gets a value indicating whether the payment method supports capture.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether capture is supported.</returns>
        public static async Task<bool> SupportCaptureAsync(this IPaymentService paymentService, string paymentMethodSystemName)
        {
            Guard.NotNull(paymentService, nameof(paymentService));

            var paymentMethod = await paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);

            return paymentMethod?.Value?.SupportCapture ?? false;
        }

        /// <summary>
        /// Checks whether a payment method is active for a shop.
        /// Note, this method does not check whether the payment type is filtered out or match applied rule sets.
        /// </summary>
        /// <param name="systemName">System name of the payment provider.</param>
        /// <param name="storeId">Filter payment provider by store identifier. 0 to load all.</param>
        /// <returns><c>True</c> payment method is active, otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<bool> IsPaymentMethodActiveAsync(this IPaymentService paymentService, string systemName, int storeId = 0)
        {
            Guard.NotNull(paymentService, nameof(paymentService));

            return await paymentService.LoadPaymentMethodBySystemNameAsync(systemName, true, storeId) != null;
        }

        /// <summary>
        /// Gets a payment method type.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A payment method type.</returns>
        public static async Task<PaymentMethodType> GetPaymentMethodTypeAsync(this IPaymentService paymentService, string paymentMethodSystemName)
        {
            Guard.NotNull(paymentService, nameof(paymentService));

            var paymentMethod = await paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);

            return paymentMethod?.Value?.PaymentMethodType ?? PaymentMethodType.Unknown;
        }

        /// <summary>
        /// Gets a recurring payment type of payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A recurring payment type of payment method.</returns>
        public static async Task<RecurringPaymentType> GetRecurringPaymentTypeAsync(this IPaymentService paymentService, string paymentMethodSystemName)
        {
            Guard.NotNull(paymentService, nameof(paymentService));

            var paymentMethod = await paymentService.LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);

            return paymentMethod?.Value?.RecurringPaymentType ?? RecurringPaymentType.NotSupported;
        }
    }
}
