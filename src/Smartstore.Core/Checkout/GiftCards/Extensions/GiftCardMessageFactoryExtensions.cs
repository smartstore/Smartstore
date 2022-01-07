using Smartstore.Core.Messaging;

namespace Smartstore.Core.Checkout.GiftCards
{
    public static class GiftCardMessageFactoryExtensions
    {
        /// <summary>
        /// Sends a gift card notification to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendGiftCardNotificationAsync(this IMessageFactory factory, GiftCard giftCard, int languageId = 0)
        {
            Guard.NotNull(giftCard, nameof(giftCard));

            var orderItem = giftCard.PurchasedWithOrderItem;
            var customer = orderItem?.Order?.Customer;
            var storeId = orderItem?.Order?.StoreId;

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.GiftCardCustomer, languageId, storeId, customer),
                true,
                giftCard,
                orderItem,
                orderItem?.Product);
        }
    }
}
