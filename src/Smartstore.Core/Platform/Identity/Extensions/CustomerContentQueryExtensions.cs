namespace Smartstore.Core.Identity
{
    public static partial class CustomerContentQueryExtensions
    {
        /// <summary>
        /// Applies a customer filter.
        /// </summary>
        /// <param name="query">Customer content query.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="approved">A value indicating whether to filter approved content.</param>
        /// <returns>Customer content query.</returns>
        public static IQueryable<CustomerContent> ApplyCustomerFilter(this IQueryable<CustomerContent> query, int? customerId = null, bool? approved = null)
        {
            Guard.NotNull(query, nameof(query));

            if (customerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }

            if (approved.HasValue)
            {
                query = query.Where(x => x.IsApproved == approved.Value);
            }

            return query;
        }
    }
}
