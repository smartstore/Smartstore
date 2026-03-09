using Smartstore.Core.Messaging;

namespace Smartstore.Core.Checkout.Orders;

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
    /// Sends a message to the store owner regarding a new return case.
    /// </summary>
    public static Task<CreateMessageResult> SendNewReturnCaseStoreOwnerNotificationAsync(this IMessageFactory factory, 
        ReturnCase returnCase, 
        OrderItem orderItem, 
        int languageId = 0)
    {
        Guard.NotNull(returnCase);
        Guard.NotNull(orderItem);

        return factory.CreateMessageAsync(
            MessageContext.Create(MessageTemplateNames.NewReturnCaseStoreOwner, languageId, orderItem.Order?.StoreId, returnCase.Customer),
            true,
            returnCase,
            orderItem,
            orderItem.Order,
            orderItem.Product);
    }

    /// <summary>
    /// Sends a message to the customer regarding a return case status change.
    /// </summary>
    public static Task<CreateMessageResult> SendReturnCaseStatusChangedCustomerNotificationAsync(this IMessageFactory factory, 
        ReturnCase returnCase, 
        OrderItem orderItem, 
        int languageId = 0)
    {
        Guard.NotNull(returnCase);
        Guard.NotNull(orderItem);

        return factory.CreateMessageAsync(
            MessageContext.Create(MessageTemplateNames.ReturnCaseStatusChangedCustomer, languageId, orderItem.Order?.StoreId, returnCase.Customer),
            true,
            returnCase,
            orderItem,
            orderItem.Order,
            orderItem.Product);
    }
}