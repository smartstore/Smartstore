using System.Threading.Tasks;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Messages;

namespace Smartstore
{
    public static class OrderMessageFactoryExtensions
    {
        /// <summary>
        /// Sends an order placed notification to the store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendOrderPlacedStoreOwnerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
        {
            Guard.NotNull(order, nameof(order));

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
            Guard.NotNull(order, nameof(order));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.OrderPlacedCustomer, languageId, order.StoreId, order.Customer), 
                true, 
                order);
        }

        /// <summary>
        /// Sends a shipment sent notification to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendShipmentSentCustomerNotificationAsync(this IMessageFactory factory, Shipment shipment, int languageId = 0)
        {
            Guard.NotNull(shipment, nameof(shipment));
            Guard.NotNull(shipment.Order, nameof(shipment.Order));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.ShipmentSentCustomer, languageId, shipment.Order.StoreId, shipment.Order.Customer), 
                true, 
                shipment, 
                shipment.Order);
        }

        /// <summary>
        /// Sends a shipment delivered notification to the customer.
        /// </summary>
        public static Task<CreateMessageResult> SendShipmentDeliveredCustomerNotificationAsync(this IMessageFactory factory, Shipment shipment, int languageId = 0)
        {
            Guard.NotNull(shipment, nameof(shipment));
            Guard.NotNull(shipment.Order, nameof(shipment.Order));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.ShipmentDeliveredCustomer, languageId, shipment.Order.StoreId, shipment.Order.Customer), 
                true, 
                shipment, 
                shipment.Order);
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
            Guard.NotNull(order, nameof(order));

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
            Guard.NotNull(orderNote, nameof(orderNote));
            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.OrderNoteAddedCustomer, languageId, orderNote.Order?.StoreId, orderNote.Order?.Customer), 
                true, 
                orderNote, 
                orderNote.Order);
        }

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

        /// <summary>
        /// Sends a new return request message to the store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendNewReturnRequestStoreOwnerNotificationAsync(this IMessageFactory factory, ReturnRequest returnRequest, OrderItem orderItem, int languageId = 0)
        {
            Guard.NotNull(returnRequest, nameof(returnRequest));
            Guard.NotNull(orderItem, nameof(orderItem));

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
            Guard.NotNull(returnRequest, nameof(returnRequest));
            Guard.NotNull(orderItem, nameof(orderItem));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.ReturnRequestStatusChangedCustomer, languageId, orderItem.Order?.StoreId, returnRequest.Customer), 
                true, 
                returnRequest);
        }

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