namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart extension methods.
    /// </summary>
    public static class ShoppingCartItemExtensions
    {
        /// <summary>
        /// Returns a filtered list of <see cref="ShoppingCartItem"/>s by <see cref="ShoppingCartType"/> and <paramref name="storeId"/>
        /// and sorts by <see cref="ShoppingCartItem.Enabled"/> descending, then by <see cref="BaseEntity.Id"/> descending.
        /// </summary>
        /// <param name="cart">The cart collection the filter gets applied on.</param>
        /// <param name="cartType"><see cref="ShoppingCartType"/> to filter by.</param>
        /// <param name="storeId">Store identifier to filter by.</param>
        /// <param name="enabled">A value indicating whether to load enabled or disabled items. <c>null</c> to load all items.</param>
        /// <param name="hasParent">A value indicating whether to load items that has a parent item. <c>null</c> to load all items.</param>
        public static IOrderedEnumerable<ShoppingCartItem> FilterByCartType(this ICollection<ShoppingCartItem> cart, 
            ShoppingCartType cartType, 
            int? storeId = null,
            bool? enabled = true,
            bool? hasParent = null)
        {
            Guard.NotNull(cart);

            var items = cart.Where(x => x.ShoppingCartTypeId == (int)cartType);

            if (storeId.GetValueOrDefault() > 0)
            {
                items = items.Where(x => x.StoreId == storeId.Value);
            }

            if (enabled != null)
            {
                items = items.Where(x => x.Enabled == enabled.Value);
            }

            if (hasParent != null)
            {
                items = hasParent == true
                    ? items.Where(x => x.ParentItemId != null)
                    : items.Where(x => x.ParentItemId == null);
            }

            return items.ApplyDefaultSorting();
        }

        /// <summary>
        /// Sorts by <see cref="ShoppingCartItem.Enabled"/> descending, then by <see cref="BaseEntity.Id"/> descending.
        /// </summary>
        public static IOrderedEnumerable<ShoppingCartItem> ApplyDefaultSorting(this IEnumerable<ShoppingCartItem> items)
        {
            return items
                .OrderByDescending(x => x.Enabled)
                .ThenByDescending(x => x.Id);
        }
    }
}