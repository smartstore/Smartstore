using Smartstore.Core.Customers;

namespace Smartstore.Core.Checkout.Cart.Events
{
    public class RenderingOrderTotalsEvent
    {
        public RenderingOrderTotalsEvent()
        {
        }

        public Customer Customer { get; set; }

        public int? StoreId { get; set; }
    }
}