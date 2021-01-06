using System.Linq;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart query extensions
    /// </summary>
    public static class ShoppingCartQueryExtensions
    {
        /// <summary>        
        /// Applies standard filter for store id and shopping cart type
        /// </summary>
        public static IQueryable<ShoppingCartItem> ApplyStandardFilter(
            this IQueryable<ShoppingCartItem> query,
            ShoppingCartType type = ShoppingCartType.ShoppingCart,
            int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (storeId > 0)
            {
                query = query.Where(x => x.StoreId == storeId);
            }

            query = query.Where(x => x.ShoppingCartTypeId == (int)type);

            return query;
        }
    }
}
