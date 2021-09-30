using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    public static class ForumQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="Forum.DisplayOrder"/>, then by <see cref="Forum.Name"/>.
        /// </summary>
        /// <param name="query">Forum query.</param>
        /// <param name="forumGroupId">Forum group identifier.</param>
        /// <returns>Forum query.</returns>
        public static IOrderedQueryable<Forum> ApplyStandardFilter(this IQueryable<Forum> query, int? forumGroupId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (forumGroupId > 0)
            {
                query = query.Where(x => x.ForumGroupId == forumGroupId.Value);
            }

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);
        }


        //public static async Task<int> ApplyStatisticsAsync(this DbSet<Forum> forums,
        //    int[] forumTopicIds,
        //    CancellationToken cancelToken = default)
        //{
        //    if (forumTopicIds.Any())
        //    {
        //        var db = forums.GetDbContext<SmartDbContext>();

        //        var query = (
        //            from ft in db.ForumTopics()
        //            join fp in db.ForumPosts() on ft.Id equals fp.TopicId
        //            where forumTopicIds.Contains(ft.Id) && ft.Published && fp.Published
        //            orderby fp.CreatedOnUtc descending, ft.CreatedOnUtc descending
        //            select ft.ForumId)
        //            .Distinct()
        //            .SelectMany(key => db.ForumPosts()
        //                .Include(x => x.ForumTopic)
        //                .ThenInclude(x => x.Forum)
        //                .Where(x => x.ForumTopic.ForumId == key)
        //                .OrderByDescending(x => x.CreatedOnUtc)
        //                .Take(1));


        //    }

        //    return 0;
        //}
    }
}
