using System.Linq;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;

namespace Smartstore
{
    /// <summary>
    /// Shopping cart query extensions
    /// </summary>
    public static class ShoppingCartQueryExtensions
    {
        /// <summary>        
        /// Applies standard filter for shopping cart item.
        /// Applies store filter, shopping cart type and customer mapping filter.
        /// </summary>
        /// <returns>
        /// Query ordered by ID
        /// </returns>
        public static IOrderedQueryable<ShoppingCartItem> ApplyStandardFilter(
            this IQueryable<ShoppingCartItem> query,
            ShoppingCartType type = ShoppingCartType.ShoppingCart,
            int storeId = 0,
            Customer customer = null)
        {
            Guard.NotNull(query, nameof(query));

            if (storeId > 0)
            {
                query = query.Where(x => x.StoreId == storeId);
            }

            if (customer != null)
            {
                query = query.Where(x => x.CustomerId == customer.Id);
            }

            return query
                .Where(x => x.ShoppingCartTypeId == (int)type)
                .OrderByDescending(x => x.Id);
        }
    }
}
