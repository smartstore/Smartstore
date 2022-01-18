using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Core.Identity;

namespace Smartstore.Forums
{
    public static partial class PrivateMessageQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="PrivateMessage.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Private message query.</param>
        /// <param name="fromCustomerId">Filter by author customer identifier.</param>
        /// <param name="toCustomerId">Filter by recipient customer identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Private message query.</returns>
        public static IOrderedQueryable<PrivateMessage> ApplyStandardFilter(
            this IQueryable<PrivateMessage> query,
            int? fromCustomerId = null,
            int? toCustomerId = null,
            int? storeId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            if (fromCustomerId.HasValue)
            {
                query = query.Where(x => x.FromCustomerId == fromCustomerId.Value);
            }

            if (toCustomerId.HasValue)
            {
                query = query.Where(x => x.ToCustomerId == toCustomerId.Value);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies a status filter.
        /// </summary>
        /// <param name="query">Private message query.</param>
        /// <param name="isRead">Filters read messages.</param>
        /// <param name="isDeletedByAuthor">Filters messages deleted by author.</param>
        /// <param name="isDeletedByRecipient">Filters messages deleted by recipient.</param>
        /// <returns>Private message query.</returns>
        public static IQueryable<PrivateMessage> ApplyStatusFilter(
            this IQueryable<PrivateMessage> query,
            bool? isRead = null,
            bool? isDeletedByAuthor = null,
            bool? isDeletedByRecipient = null)
        {
            Guard.NotNull(query, nameof(query));

            if (isRead.HasValue)
            {
                query = query.Where(x => x.IsRead == isRead.Value);
            }

            if (isDeletedByAuthor.HasValue)
            {
                query = query.Where(x => x.IsDeletedByAuthor == isDeletedByAuthor.Value);
            }

            if (isDeletedByRecipient.HasValue)
            {
                query = query.Where(x => x.IsDeletedByRecipient == isDeletedByRecipient.Value);
            }

            return query;
        }

        /// <summary>
        /// Includes <see cref="PrivateMessage.FromCustomer"/>, <see cref="PrivateMessage.ToCustomer"/> and its
        /// <see cref="Customer.CustomerRoleMappings"/> and <see cref="CustomerRoleMapping.CustomerRole"/> for eager loading.
        /// </summary>
        public static IIncludableQueryable<PrivateMessage, CustomerRole> IncludeCustomers(this IQueryable<PrivateMessage> query)
        {
            Guard.NotNull(query, nameof(query));

            return query
                .AsSplitQuery()
                .Include(x => x.FromCustomer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .Include(x => x.ToCustomer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole);
        }
    }
}
