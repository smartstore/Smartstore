namespace Smartstore.Core.Catalog.Products
{
    public static partial class BackInStockSubscriptionQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="BackInStockSubscription.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Back in stock subscription query.</param>
        /// <param name="productId">Product identifier.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        public static IQueryable<BackInStockSubscription> ApplyStandardFilter(
            this IQueryable<BackInStockSubscription> query,
            int? productId = null,
            int? customerId = null,
            int? storeId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (productId > 0)
            {
                query = query.Where(x => x.ProductId == productId.Value);
            }

            if (customerId > 0)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }

            if (storeId > 0)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }
    }
}
