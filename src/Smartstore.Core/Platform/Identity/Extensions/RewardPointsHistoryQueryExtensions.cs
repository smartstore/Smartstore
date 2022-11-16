namespace Smartstore.Core.Identity
{
    public static partial class RewardPointsHistoryQueryExtensions
    {
        /// <summary>
        /// Applies a customer filter and sorts by <see cref="RewardPointsHistory.CustomerId"/>, then by <see cref="RewardPointsHistory.CreatedOnUtc"/> descending and 
        /// then by <see cref="BaseEntity.Id"/> descending.
        /// </summary>
        /// <param name="query">Rewardpoints history query.</param>
        /// <param name="customerIds">Customer identifiers.</param>
        /// <returns>Rewardpoints history query.</returns>
        public static IOrderedQueryable<RewardPointsHistory> ApplyCustomerFilter(this IQueryable<RewardPointsHistory> query, int[] customerIds)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(customerIds, nameof(customerIds));

            query = query.Where(x => customerIds.Contains(x.CustomerId));

            return query
                .OrderBy(x => x.CustomerId)
                .ThenByDescending(x => x.CreatedOnUtc)
                .ThenByDescending(x => x.Id);
        }
    }
}
