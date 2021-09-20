using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    public static partial class ForumPostQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="ForumPost.CreatedOnUtc"/>, then by <see cref="ForumPost.Id"/>.
        /// </summary>
        /// <param name="query">Forum post query.</param>
        /// <param name="customer">Customer, usually the current customer.</param>
        /// <param name="forumTopicId">Filter by forum topic identifier.</param>
        /// <param name="includeHidden">Applies filter by <see cref="ForumPost.Published"/> if <paramref name="customer"/> is not a forum moderator.</param>
        /// <param name="sortDesc">A value indicating whether to sort descending or ascending.</param>
        /// <returns>Forum post query.</returns>
        public static IOrderedQueryable<ForumPost> ApplyStandardFilter(
            this IQueryable<ForumPost> query,
            Customer customer,
            int? forumTopicId = null,
            bool includeHidden = false,
            bool sortDesc = false)
        {
            Guard.NotNull(query, nameof(query));

            if (forumTopicId.HasValue)
            {
                query = query.Where(x => x.TopicId == forumTopicId.Value);
            }

            if (!includeHidden && !customer.IsForumModerator())
            {
                query = query.Where(x => x.Published || x.CustomerId == customer.Id);
            }

            if (sortDesc)
            {
                return query
                    .OrderByDescending(x => x.CreatedOnUtc)
                    .ThenBy(x => x.Id);
            }
            else
            {
                return query
                    .OrderBy(x => x.CreatedOnUtc)
                    .ThenBy(x => x.Id);
            }
        }

        /// <summary>
        /// Applies a store filter through associated <see cref="ForumGroup"/>.
        /// </summary>
        /// <param name="query">Forum post query.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Forum post query.</returns>
        public static IQueryable<ForumPost> ApplyStoreFilter(this IQueryable<ForumPost> query, int storeId)
        {
            Guard.NotNull(query, nameof(query));

            var db = query.GetDbContext<SmartDbContext>();
            if (storeId == 0 || db.QuerySettings.IgnoreMultiStore)
            {
                return query;
            }

            query =
                from fp in query
                join sm in db.StoreMappings.AsNoTracking() on new { eid = fp.ForumTopic.Forum.ForumGroupId, ename = "ForumGroup" }
                equals new { eid = sm.EntityId, ename = sm.EntityName } into fpsm
                from sm in fpsm.DefaultIfEmpty()
                where !fp.ForumTopic.Forum.ForumGroup.LimitedToStores || sm.StoreId == storeId
                select fp;

            return query;
        }
    }
}
