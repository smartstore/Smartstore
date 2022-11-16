using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;

namespace Smartstore
{
    public static partial class ReturnRequestQueryExtensions
    {
        /// <summary>
        /// Includes the the customer graph for eager loading.
        /// </summary>
        public static IIncludableQueryable<ReturnRequest, CustomerRole> IncludeCustomer(this IQueryable<ReturnRequest> query,
            bool includeAddresses = true)
        {
            Guard.NotNull(query, nameof(query));

            if (includeAddresses)
            {
                query = query
                    .Include(x => x.Customer).ThenInclude(x => x.BillingAddress)
                    .Include(x => x.Customer).ThenInclude(x => x.ShippingAddress);
            }

            return query
                .AsSplitQuery()
                .Include(x => x.Customer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole);
        }

        /// <summary>
        /// Applies a standard filter and sorts by <see cref="ReturnRequest.CreatedOnUtc"/> descending, then by <see cref="BaseEntity.Id"/> descending.
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
