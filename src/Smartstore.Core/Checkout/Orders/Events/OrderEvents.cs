namespace Smartstore.Core.Checkout.Orders.Events
{
    public class OrderPaidEvent
    {
        public OrderPaidEvent(Order order)
        {
            Order = Guard.NotNull(order);
        }

        public Order Order { get; init; }
    }

    public class OrderPlacedEvent
    {
        public OrderPlacedEvent(Order order)
        {
            Order = Guard.NotNull(order);
        }

        public Order Order { get; init; }
    }

    public class OrderUpdatedEvent
    {
        public OrderUpdatedEvent(Order order)
        {
            Order = Guard.NotNull(order);
        }

        public Order Order { get; init; }
    }
}