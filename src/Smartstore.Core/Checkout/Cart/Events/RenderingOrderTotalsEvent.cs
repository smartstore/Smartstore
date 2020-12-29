using Smartstore.Core.Customers;

namespace Smartstore.Core.Checkout.Cart.Events
{
    public class RenderingOrderTotalsEvent
    {
        public Customer Customer { get; init; }

        public int? StoreId { get; init; }
    }
}