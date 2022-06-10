using Smartstore.Core.Messaging;

namespace Smartstore.Core.Checkout.Payment
{
    public static class PaymentMessageFactoryExtensions
    {
        /// <summary>
        /// Sends a recurring payment cancelled notification to the store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendRecurringPaymentCancelledStoreOwnerNotificationAsync(this IMessageFactory factory, RecurringPayment recurringPayment, int languageId = 0)
        {
            Guard.NotNull(recurringPayment, nameof(recurringPayment));

            var order = recurringPayment.InitialOrder;
            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.RecurringPaymentCancelledStoreOwner, languageId, order?.StoreId, order?.Customer),
                true,
                recurringPayment,
                order);
        }
    }
}
