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
        public ShoppingCart(OrganizedShoppingCartItem[] items)
        {
            Guard.NotNull(items, nameof(items));

            Items = items;
        }

        /// <summary>
        /// Array of cart items.
        /// </summary>
        public OrganizedShoppingCartItem[] Items { get; private set; }

        /// <summary>
        /// Shopping cart type.
        /// </summary>
        public ShoppingCartType CartType { get; init; } = ShoppingCartType.ShoppingCart;

        /// <summary>
        /// Customer of the cart.
        /// </summary>
        public Customer Customer { get; init; }

        /// <summary>
        /// Store identifier.
        /// </summary>
        public int StoreId { get; init; }
    }
}
