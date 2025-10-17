using Smartstore.Core.Identity;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Cart.Events
{
    /// <summary>
    /// Represents a shopping cart migration event.
    /// </summary>
    public class MigrateShoppingCartEvent : IEventMessage
    {
        public MigrateShoppingCartEvent(Customer fromCustomer, Customer toCustomer, int storeId)
        {
            Guard.NotNull(fromCustomer, nameof(fromCustomer));
            Guard.NotNull(toCustomer, nameof(toCustomer));

            FromCustomer = fromCustomer;
            ToCustomer = toCustomer;
            StoreId = storeId;
        }

        /// <summary>
        /// Gets the customer to migrate from.
        /// </summary>
        public Customer FromCustomer { get; init; }

        /// <summary>
        /// Gets the customer to migrate to.
        /// </summary>
        public Customer ToCustomer { get; init; }

        /// <summary>
        /// Gets the store identifier.
        /// </summary>
        public int StoreId { get; init; }
    }
}
