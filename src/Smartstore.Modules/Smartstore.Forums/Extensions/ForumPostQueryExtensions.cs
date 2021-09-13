using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    internal static partial class ForumPostQueryExtensions
    {
        /// <summary>
        /// Applies a filter for store through associated <see cref="ForumGroup"/>.
        /// </summary>
        /// <param name="query">Forum post query.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Forum post query.</returns>
        internal static IQueryable<ForumPost> ApplyStoreFilter(this IQueryable<ForumPost> query, int storeId)
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
