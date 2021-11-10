using System.Linq;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    public static partial class ReturnRequestQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="ReturnRequest.CreatedOnUtc"/> descending, then by <see cref="ReturnRequest.Id"/> descending.
        /// </summary>
        /// <param name="query">Return request query.</param>
        /// <param name="orderItemIds">Order item identifiers.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Return request query.</returns>
        public static IOrderedQueryable<ReturnRequest> ApplyStandardFilter(this IQueryable<ReturnRequest> query, 
            int[] orderItemIds = null,
            int? customerId = null, 
            int? storeId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (orderItemIds?.Any() ?? false)
            {
                query = query.Where(x => orderItemIds.Contains(x.OrderItemId));
            }

            // INFO: (mg) (core) A nullable comparer is smart enough, GetValueOrDefault() can be omitted.
            if (customerId > 0)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }

            if (storeId > 0)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            return query
                .OrderByDescending(x => x.CreatedOnUtc)
                .ThenByDescending(x => x.Id);
        }
    }
}
