namespace Smartstore.Core.Checkout.Orders.Events
{
    public class OrderPaidEvent(Order order)
    {
        public Order Order { get; init; } = Guard.NotNull(order);
    }

    public class OrderPlacedEvent(Order order)
    {
        public Order Order { get; init; } = Guard.NotNull(order);
    }

    public class OrderUpdatedEvent(Order order)
    {
        public Order Order { get; init; } = Guard.NotNull(order);
    }
}