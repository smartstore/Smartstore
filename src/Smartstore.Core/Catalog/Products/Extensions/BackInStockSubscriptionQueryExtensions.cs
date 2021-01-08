using System.Linq;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class BackInStockSubscriptionQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="BackInStockSubscription.CreatedOnUtc"/> DESC.
        /// </summary>
        /// <param name="query">Back in stock subscription query.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="productId">Product identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Back in stock subscription query.</returns>
        public static IQueryable<BackInStockSubscription> ApplyStandardFilter(
            this IQueryable<BackInStockSubscription> query,
            int customerId = 0,
            int productId = 0,
            int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (customerId != 0)
            {
                query = query.Where(x => x.CustomerId == customerId);
            }

            if (productId != 0)
            {
                query = query.Where(x => x.ProductId == productId);
            }

            if (storeId != 0)
            {
                query = query.Where(x => x.StoreId == storeId);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }
    }
}
