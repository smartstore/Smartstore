namespace Smartstore.Core.Checkout.Orders.Events
{
    public class OrderPlacedEvent
    {
        public OrderPlacedEvent(Order order)
        {
            Guard.NotNull(order, nameof(order));

            Order = order;
        }

        public Order Order { get; init; }
    }
}