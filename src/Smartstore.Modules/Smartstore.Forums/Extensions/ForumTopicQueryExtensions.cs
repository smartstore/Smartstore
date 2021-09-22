using System.Linq;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    public static partial class ForumTopicQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="ForumTopic.TopicTypeId"/> descending, then by <see cref="ForumTopic.LastPostTime"/> descending,
        /// then by <see cref="ForumTopic.Id"/> descending.
        /// </summary>
        /// <param name="query">Forum topic query.</param>
        /// <param name="customer">Customer, usually the current customer.</param>
        /// <param name="includeHidden">Applies filter by <see cref="ForumTopic.Published"/> if <paramref name="customer"/> is not a forum moderator.</param>
        /// <returns>Forum topic query.</returns>
        public static IOrderedQueryable<ForumTopic> ApplyStandardFilter(
            this IQueryable<ForumTopic> query,
            Customer customer,
            bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(customer, nameof(customer));

            if (!includeHidden && !customer.IsForumModerator())
            {
                query = query.Where(x => x.Published || x.CustomerId == customer.Id);
            }

            return query.OrderByDescending(x => x.TopicTypeId)
                .ThenByDescending(x => x.LastPostTime)
                .ThenByDescending(x => x.Id);
        }

        /// <summary>
        /// Applies a filter for active topics and sorts by <see cref="ForumTopic.LastPostTime"/> descending.
        /// </summary>
        /// <param name="query">Forum topic query.</param>
        /// <param name="store">Filter by store, usually <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <param name="customer">Filter by customer, usually <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="forumId">Filter by forum identifier.</param>
        /// <returns>Forum topic query.</returns>
        public static IOrderedQueryable<ForumTopic> ApplyActiveFilter(
            this IQueryable<ForumTopic> query,
            Store store,
            Customer customer,
            int? forumId = null)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(store, nameof(store));
            Guard.NotNull(customer, nameof(customer));

            var db = query.GetDbContext<SmartDbContext>();

            var groupIdsQuery = db.ForumGroups()
                .ApplyStoreFilter(store.Id)
                .ApplyAclFilter(customer)
                .Select(x => x.Id);

            query = query.Where(x => x.LastPostTime.HasValue);

            if (forumId.HasValue)
            {
                query = query.Where(x => x.ForumId == forumId.Value);
            }

            query = query.Where(x => groupIdsQuery.Contains(x.Forum.ForumGroupId));

            if (!customer.IsForumModerator())
            {
                query = query.Where(x => x.Published || x.CustomerId == customer.Id);
            }

            return query.OrderByDescending(x => x.LastPostTime);
        }
    }
}
