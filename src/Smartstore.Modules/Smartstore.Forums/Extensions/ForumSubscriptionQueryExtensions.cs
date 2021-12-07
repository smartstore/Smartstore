using Smartstore.Core;

namespace Smartstore.Forums
{
    public static partial class ForumSubscriptionQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="ForumSubscription.CreatedOnUtc"/> descending, 
        /// then by <see cref="ForumSubscription.SubscriptionGuid"/> descending.
        /// </summary>
        /// <param name="query">Forum subscription query.</param>
        /// <param name="customerId">Filter by customer identifier, usually <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="forumId">Filter by forum identifier.</param>
        /// <param name="forumTopicId">Filter by forum topic identifier.</param>
        /// <returns>Forum subscription query.</returns>
        public static IOrderedQueryable<ForumSubscription> ApplyStandardFilter(
            this IQueryable<ForumSubscription> query,
            int? customerId = null,
            int? forumId = null,
            int? forumTopicId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (customerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }
            if (forumId.HasValue)
            {
                query = query.Where(x => x.ForumId == forumId.Value);
            }
            if (forumTopicId.HasValue)
            {
                query = query.Where(x => x.TopicId == forumTopicId.Value);
            }

            query = query.Where(x => x.Customer.Active);

            return query
                .OrderByDescending(x => x.CreatedOnUtc)
                .ThenByDescending(x => x.SubscriptionGuid);
        }
    }
}
