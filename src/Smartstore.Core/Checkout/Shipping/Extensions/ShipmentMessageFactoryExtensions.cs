using Smartstore.Core.Messaging;

namespace Smartstore.Core.Checkout.Shipping
{
    public static partial class ShipmentMessageFactoryExtensions
    {
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
    }
}
