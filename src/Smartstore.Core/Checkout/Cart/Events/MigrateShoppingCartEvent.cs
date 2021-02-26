using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart.Events
{
    public class MigrateShoppingCartEvent
    {
        public MigrateShoppingCartEvent(Customer fromCustomer, Customer toCustomer, int storeId)
        {
            Guard.NotNull(fromCustomer, nameof(fromCustomer));
            Guard.NotNull(toCustomer, nameof(toCustomer));

            FromCustomer = fromCustomer;
            ToCustomer = toCustomer;
            StoreId = storeId;
        }

        public Customer FromCustomer { get; init; }

        public Customer ToCustomer { get; init; }

        public int StoreId { get; init; }
    }
}
