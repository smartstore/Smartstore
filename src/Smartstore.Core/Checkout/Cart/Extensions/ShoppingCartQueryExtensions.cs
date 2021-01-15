using System.Linq;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Customers;

namespace Smartstore
{
    /// <summary>
    /// Shopping cart query extensions
    /// </summary>
    public static class ShoppingCartQueryExtensions
    {
        /// <summary>        
        /// Applies standard filter for store id, shopping cart type and customer mapping
        /// </summary>
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

            query = query.Where(x => x.ShoppingCartTypeId == (int)type);

            return query.OrderByDescending(x => x.Id);
        }
    }
}
