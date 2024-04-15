using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart item query extensions
    /// </summary>
    public static partial class ShoppingCartItemQueryExtensions
    {
        /// <summary>        
        /// Applies standard filter for shopping cart item.
        /// Applies store filter, shopping cart type and customer mapping filter.
        /// </summary>
        /// <returns>Query ordered by ID</returns>
        public static IOrderedQueryable<ShoppingCartItem> ApplyStandardFilter(
            this IQueryable<ShoppingCartItem> query,
            ShoppingCartType type = ShoppingCartType.ShoppingCart,
            int storeId = 0,
            Customer customer = null)
        {
            Guard.NotNull(query);

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
                .OrderBy(x => x.Id);
        }

        /// <summary>
        /// Applies expired shopping cart items filter.
        /// Filters all items with a date time older than <paramref name="olderThanUtc"/>.
        /// </summary>
        /// <param name="olderThanUtc">Expiry threshold date time.</param>
        /// <param name="customer">Customer of cart. Can be null to apply for all customers.</param>
        /// <returns>Filtered query with no expired cart items.</returns>
        public static IQueryable<ShoppingCartItem> ApplyExpiredCartItemsFilter(this IQueryable<ShoppingCartItem> query, DateTime olderThanUtc, Customer customer = null)
        {
            query = query.Where(x => x.UpdatedOnUtc < olderThanUtc && x.ParentItemId == null);

            if (customer != null)
            {
                query = query.Where(x => x.CustomerId == customer.Id);
            }

            return query;
        }

        /// <summary>
        /// Gets the customers shopping cart items count async.
        /// </summary>
        /// <param name="customer">Customer of cart to be counted.</param>
        /// <param name="cartType">Shopping cart type.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Number of items.</returns>
        public static Task<int> CountCartItemsAsync(this IQueryable<ShoppingCartItem> query,
            Customer customer,
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart,
            int storeId = 0)
        {
            var db = query.GetDbContext<SmartDbContext>();
            if (db.IsCollectionLoaded(customer, x => x.ShoppingCartItems))
            {
                var cartItems = customer.ShoppingCartItems
                    .Where(x => x.ParentItemId == null && x.ShoppingCartTypeId == (int)cartType);

                if (customer != null)
                {
                    cartItems = cartItems.Where(x => x.CustomerId == customer.Id);
                }

                if (storeId > 0)
                {
                    cartItems = cartItems.Where(x => x.StoreId == storeId);
                }

                return Task.FromResult(cartItems.Sum(x => x.Quantity));
            }

            return query
                .ApplyStandardFilter(cartType, storeId, customer)
                .Where(x => x.ParentItemId == null)
                .SumAsync(x => (int?)x.Quantity ?? 0);
        }

        /// <summary>
        /// Gets all open sub totals of defined <see cref="ShoppingCartType"/>.
        /// Filters query for entires with matching <see cref="ShoppingCartType"/> and where the product is not null.
        /// </summary>
        /// <param name="query">Shopping cart item query to get sum.</param>
        /// <param name="cartType"><see cref="ShoppingCartType"/> of shopping cart items.</param>
        /// <param name="active">A value indicating whether to only load active/inactive items. <c>null</c> to load all items.</param>
        /// <returns>Sub total of all open wish lists.</returns>
        public static Task<decimal> GetOpenCartTypeSubTotalAsync(this IQueryable<ShoppingCartItem> query, 
            ShoppingCartType cartType = ShoppingCartType.ShoppingCart,
            bool? active = null)
        {
            return query
                .Where(x => x.ShoppingCartTypeId == (int)cartType && x.Product != null && (active == null || x.Active == active.Value))
                .SumAsync(x => (decimal?)(x.Product.Price * x.Quantity) ?? decimal.Zero);
        }
    }
}
