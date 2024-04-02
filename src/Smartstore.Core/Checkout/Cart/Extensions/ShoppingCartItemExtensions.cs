namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart extension methods.
    /// </summary>
    public static class ShoppingCartItemExtensions
    {
        /// <summary>
        /// Returns a filtered list of <see cref="ShoppingCartItem"/>s by <see cref="ShoppingCartType"/> and <paramref name="storeId"/>
        /// and sorts by <see cref="BaseEntity.Id"/> descending.
        /// </summary>
        /// <param name="cart">The cart collection the filter gets applied on.</param>
        /// <param name="cartType"><see cref="ShoppingCartType"/> to filter by.</param>
        /// <param name="storeId">Store identifier to filter by.</param>
        /// <param name="enabled">A value indicating whether to load enabled or disabled items. <c>null</c> to load all items.</param>
        /// <returns><see cref="List{T}"/> of <see cref="ShoppingCartItem"/>.</returns>
        public static IList<ShoppingCartItem> FilterByCartType(this ICollection<ShoppingCartItem> cart, 
            ShoppingCartType cartType, 
            int? storeId = null,
            bool? enabled = true)
        {
            Guard.NotNull(cart);

            // INFO: ICollection<ShoppingCartItem> indicates that this is a POST-query filter.

            var items = cart.Where(x => x.ShoppingCartTypeId == (int)cartType);

            if (storeId.GetValueOrDefault() > 0)
            {
                items = items.Where(x => x.StoreId == storeId.Value);
            }

            if (enabled != null)
            {
                items = items.Where(x => x.Enabled == enabled.Value);
            }

            return items.OrderByDescending(x => x.Id).ToList();
        }
    }
}