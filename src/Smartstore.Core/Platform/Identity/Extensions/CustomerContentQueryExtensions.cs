using System;
using System.Linq;

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

        /// <summary>
        /// Applies a time filter and sorts by <see cref="CustomerContent.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Customer content query.</param>
        /// <param name="startTime">Start time in UTC.</param>
        /// <param name="endTime">End time in UTC.</param>
        /// <returns>Customer content query.</returns>
        public static IOrderedQueryable<CustomerContent> ApplyTimeFilter(this IQueryable<CustomerContent> query, DateTime? startTime = null, DateTime? endTime = null)
        {
            Guard.NotNull(query, nameof(query));

            if (startTime.HasValue)
            {
                query = query.Where(x => x.CreatedOnUtc >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(x => x.CreatedOnUtc <= endTime.Value);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }
    }
}
