namespace Smartstore.Core.Orders.Events
{
    public class OrderUpdatedEvent
    {
        public OrderUpdatedEvent(Order order)
        {
            Guard.NotNull(order, nameof(order));

            Order = order;
        }

        public Order Order { get; init; }
    }
}
