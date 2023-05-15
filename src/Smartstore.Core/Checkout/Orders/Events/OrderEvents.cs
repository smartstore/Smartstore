using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders.Events
{
    public class OrderPaidEvent
    {
        public OrderPaidEvent(Order order)
        {
            Order = Guard.NotNull(order, nameof(order));
        }

        public Order Order { get; init; }
    }

    public class OrderPlacedEvent
    {
        public OrderPlacedEvent(Order order)
        {
            Order = Guard.NotNull(order, nameof(order));
        }

        public Order Order { get; init; }
    }

    public class OrderUpdatedEvent
    {
        public OrderUpdatedEvent(Order order)
        {
            Order = Guard.NotNull(order, nameof(order));
        }

        public Order Order { get; init; }
    }

    public class OrderItemAddedEvent
    {
        public OrderItemAddedEvent(OrderItem addedItem, OrganizedShoppingCartItem addedCartItem)
        {
            AddedItem = Guard.NotNull(addedItem, nameof(addedItem));
            AddedCartItem = Guard.NotNull(addedCartItem, nameof(addedCartItem));
        }

        public OrderItem AddedItem { get; init; }
        public OrganizedShoppingCartItem AddedCartItem { get; init; }
    }
}