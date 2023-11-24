using Smartstore.Core.Messaging;

namespace Smartstore.Core.Checkout.Orders
{
    public static class OrderMessageFactoryExtensions
    {
        /// <summary>
        /// Sends an order placed notification to the store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendOrderPlacedStoreOwnerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
        {
            Guard.NotNull(order);

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.OrderPlacedStoreOwner, languageId, order.StoreId),
                true,
                order,
                order.Customer);
        }

        /// <summary>
        /// Sends an order placed notification to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendOrderPlacedCustomerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
        {
            Guard.NotNull(order);

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.OrderPlacedCustomer, languageId, order.StoreId, order.Customer),
                true,
                order);
        }

        /// <summary>
        /// Sends an order completed notification to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendOrderCompletedCustomerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
        {
            Guard.NotNull(order, nameof(order));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.OrderCompletedCustomer, languageId, order.StoreId, order.Customer),
                true,
                order);
        }

        /// <summary>
        /// Sends an order cancelled notification to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendOrderCancelledCustomerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
        {
            Guard.NotNull(order);

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.OrderCancelledCustomer, languageId, order.StoreId, order.Customer),
                true,
                order);
        }

        /// <summary>
        /// Sends a new order note added notification to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendNewOrderNoteAddedCustomerNotificationAsync(this IMessageFactory factory, OrderNote orderNote, int languageId = 0)
        {
            Guard.NotNull(orderNote);

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.OrderNoteAddedCustomer, languageId, orderNote.Order?.StoreId, orderNote.Order?.Customer),
                true,
                orderNote,
                orderNote.Order);
        }

        /// <summary>
        /// Sends a new return request message to the store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendNewReturnRequestStoreOwnerNotificationAsync(this IMessageFactory factory, ReturnRequest returnRequest, OrderItem orderItem, int languageId = 0)
        {
            Guard.NotNull(returnRequest);
            Guard.NotNull(orderItem);

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.NewReturnRequestStoreOwner, languageId, orderItem.Order?.StoreId, returnRequest.Customer),
                true,
                returnRequest,
                orderItem,
                orderItem.Order,
                orderItem.Product);
        }

        /// <summary>
        /// Sends a return request status changed message to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendReturnRequestStatusChangedCustomerNotificationAsync(this IMessageFactory factory, ReturnRequest returnRequest, OrderItem orderItem, int languageId = 0)
        {
            Guard.NotNull(returnRequest);
            Guard.NotNull(orderItem);

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.ReturnRequestStatusChangedCustomer, languageId, orderItem.Order?.StoreId, returnRequest.Customer),
                true,
                returnRequest,
                orderItem,
                orderItem.Order,
                orderItem.Product);
        }
    }
}