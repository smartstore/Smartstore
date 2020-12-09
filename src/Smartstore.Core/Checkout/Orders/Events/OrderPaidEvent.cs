namespace Smartstore.Core.Orders.Events
{
    public class OrderPaidEvent
    {
        public OrderPaidEvent(Order order)
        {
            Guard.NotNull(order, nameof(order));

            Order = order;
        }

        public Order Order { get; init; }
    }
}