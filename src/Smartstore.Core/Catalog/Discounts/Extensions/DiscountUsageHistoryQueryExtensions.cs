namespace Smartstore.Core.Catalog.Discounts
{
    public static partial class DiscountUsageHistoryQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="DiscountUsageHistory.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Discount usage history query.</param>
        /// <param name="discountId">Discount identifier.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <returns>Ordered discount usage history query.</returns>
        public static IOrderedQueryable<DiscountUsageHistory> ApplyStandardFilter(this IQueryable<DiscountUsageHistory> query, int? discountId = null, int? customerId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (discountId.HasValue)
            {
                query = query.Where(x => x.DiscountId == discountId.Value);
            }

            if (customerId.HasValue)
            {
                query = query.Where(x => x.Order != null && x.Order.CustomerId == customerId.Value);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }
    }
}
