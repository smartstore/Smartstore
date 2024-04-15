namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents an organized shopping cart item.
    /// </summary>
    public partial class OrganizedShoppingCartItem : IEquatable<OrganizedShoppingCartItem>
    {
        public OrganizedShoppingCartItem(ShoppingCartItem item, bool? active = null)
        {
            Item = Guard.NotNull(item);
            Active = active ?? item.Active;
        }

        /// <summary>
        /// Gets the shopping cart item.
        /// </summary>
        public ShoppingCartItem Item { get; }

        /// <summary>
        /// Gets a value indicating whether the cart item is active.
        /// May differ from <see cref="ShoppingCartItem.Active"/> and has precedence over it.
        /// Always <c>true</c> if <see cref="ShoppingCartSettings.AllowActivatableCartItems"/> is <c>false</c>.
        /// </summary>
        public bool Active { get; }

        /// <summary>
        /// Gets or sets the list of child items.
        /// </summary>
        public List<OrganizedShoppingCartItem> ChildItems { get; } = [];

        /// <summary>
        /// Gets or sets custom properties dictionary.
        /// Use this for any custom data required along cart processing.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = [];

        #region Compare

        public override bool Equals(object obj)
            => Equals(obj as OrganizedShoppingCartItem);

        bool IEquatable<OrganizedShoppingCartItem>.Equals(OrganizedShoppingCartItem other)
            => Equals(other);

        protected virtual bool Equals(OrganizedShoppingCartItem other)
            => Item.Equals(other.Item);

        public override int GetHashCode()
            => Item.GetHashCode();

        #endregion
    }
}