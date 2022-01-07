using System.Diagnostics;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a shopping cart.
    /// </summary>
    [DebuggerDisplay("{CartType} for {Customer.Email} contains {Items.Length} items.")]
    public partial class ShoppingCart
    {
        public ShoppingCart(Customer customer, int storeId, IEnumerable<OrganizedShoppingCartItem> items)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(items, nameof(items));

            Customer = customer;
            StoreId = storeId;
            Items = items.ToArray();
        }

        /// <summary>
        /// Array of cart items.
        /// </summary>
        public OrganizedShoppingCartItem[] Items { get; }

        /// <summary>
        /// A value indicating whether the cart contains cart items.
        /// </summary>
        public bool HasItems
            => Items.Length > 0;

        /// <summary>
        /// Shopping cart type.
        /// </summary>
        public ShoppingCartType CartType { get; init; } = ShoppingCartType.ShoppingCart;

        /// <summary>
        /// Customer of the cart.
        /// </summary>
        public Customer Customer { get; }

        /// <summary>
        /// Store identifier.
        /// </summary>
        public int StoreId { get; }
    }
}
