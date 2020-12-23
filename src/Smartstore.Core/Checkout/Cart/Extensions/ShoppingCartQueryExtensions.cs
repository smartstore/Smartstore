using System.Linq;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart query extensions
    /// </summary>
    public static class ShoppingCartQueryExtensions
    {
        /// <summary>
        /// Standard filter for shopping cart.
        /// Applies store filter and type, may include hidden (<see cref="CheckoutAttribute.IsActive"/>) attributes 
        /// and orders query by <see cref="CheckoutAttribute.DisplayOrder"/>
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
