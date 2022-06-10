namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents an organized shopping cart item.
    /// </summary>
    public partial class OrganizedShoppingCartItem
    {
        public OrganizedShoppingCartItem(ShoppingCartItem item)
        {
            Guard.NotNull(item, nameof(item));

            Item = item;
        }

        /// <summary>
        /// Gets the shopping cart item.
        /// </summary>
        public ShoppingCartItem Item { get; init; }

        /// <summary>
        /// Gets or sets the list of child items.
        /// </summary>
        public List<OrganizedShoppingCartItem> ChildItems { get; set; } = new();

        /// <summary>
        /// Gets or sets custom properties dictionary.
        /// Use this for any custom data required along cart processing.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();
    }
}