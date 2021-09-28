using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Core;
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
        /// <param name="customer">Filter by customer, usually <see cref="IWorkContext.CurrentCustomer"/>.</param>
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

        /// <summary>
        /// Includes <see cref="ForumPost.Customer"/>, <see cref="Customer.CustomerRoleMappings"/> and 
        /// <see cref="CustomerRoleMapping.CustomerRole"/> for eager loading.
        /// </summary>
        public static IIncludableQueryable<ForumPost, CustomerRole> IncludeCustomer(this IQueryable<ForumPost> query)
        {
            Guard.NotNull(query, nameof(query));

            return query
                .Include(x => x.Customer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole);
        }

        public static async Task<Dictionary<int, ForumPost>> GetForumPostsByIdsAsync(this DbSet<ForumPost> forumPosts, IEnumerable<int> forumPostIds)
        {
            var ids = forumPostIds
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            if (ids.Any())
            {
                return await forumPosts
                    .Include(x => x.ForumTopic)
                    .IncludeCustomer()
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);
            }
            else
            {
                return new Dictionary<int, ForumPost>();
            }
        }

        public static async Task<int> GetTopicPageIndexAsync(this DbSet<ForumPost> forumPosts, 
            Customer customer,
            int topicId, 
            int pageSize, 
            int postId)
        {
            if (pageSize > 0 && postId != 0)
            {
                var postIds = await forumPosts
                    .AsNoTracking()
                    .ApplyStandardFilter(customer, topicId, true)
                    .Select(x => x.Id)
                    .ToListAsync();
                    
                for (var i = 0; i < postIds.Count; ++i)
                {
                    if (postIds[i] == postId)
                    {
                        return i / pageSize;
                    }
                }
            }

            return 0;
        }

    }
}
