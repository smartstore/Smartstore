namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart extension methods.
    /// </summary>
    public static class ShoppingCartItemExtensions
    {
        /// <summary>
        /// Returns a filtered list of <see cref="ShoppingCartItem"/>s by <see cref="ShoppingCartType"/> and <paramref name="storeId"/>
        /// and sorts by <see cref="BaseEntity.Id"/> ascending.
        /// </summary>
        /// <param name="cart">The cart collection the filter gets applied on.</param>
        /// <param name="cartType"><see cref="ShoppingCartType"/> to filter by.</param>
        /// <param name="storeId">Store identifier to filter by.</param>
        /// <param name="active">A value indicating whether to load active items. <c>null</c> to load all items.</param>
        /// <param name="hasParent">A value indicating whether to load items that has a parent item. <c>null</c> to load all items.</param>
        public static IOrderedEnumerable<ShoppingCartItem> FilterByCartType(this ICollection<ShoppingCartItem> cart, 
            ShoppingCartType cartType, 
            int? storeId = null,
            bool? active = true,
            bool? hasParent = null)
        {
            Guard.NotNull(cart);

            var items = cart.Where(x => x.ShoppingCartTypeId == (int)cartType);

            if (storeId.GetValueOrDefault() > 0)
            {
                items = items.Where(x => x.StoreId == storeId.Value);
            }

            if (active != null)
            {
                items = items.Where(x => x.Active == active.Value);
            }

            if (hasParent != null)
            {
                items = hasParent == true
                    ? items.Where(x => x.ParentItemId != null)
                    : items.Where(x => x.ParentItemId == null);
            }

            return items.OrderBy(x => x.Id);
        }
    }
}