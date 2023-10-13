using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a single line in the shopping cart. At the moment this is always a product.
    /// </summary>
    public partial class ShoppingCartLineItem
    {
        public ShoppingCartLineItem(OrganizedShoppingCartItem item)
        {
            Item = Guard.NotNull(item);
        }

        /// <summary>
        /// The shopping cart item.
        /// </summary>
        public OrganizedShoppingCartItem Item { get; private set; }

        /// <summary>
        /// The calculated unit price in the primary currency.
        /// </summary>
        public CalculatedPrice UnitPrice { get; init; }

        /// <summary>
        /// The calculated subtotal of the line item in the primary currency.
        /// It is the <see cref="UnitPrice"/> multiplied by <see cref="ShoppingCartItem.Quantity"/>.
        /// </summary>
        public CalculatedPrice Subtotal { get; init; }
    }
}
