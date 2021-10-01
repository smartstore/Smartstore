using System.Collections.Generic;
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


        public static async Task<int> ApplyStatisticsAsync(this DbSet<Forum> forums,
            int[] forumIds,
            CancellationToken cancelToken = default)
        {
            if (forumIds.Any())
            {
                var db = forums.GetDbContext<SmartDbContext>();
                var postSet = db.ForumPosts();
                var topicSet = db.ForumTopics();

                var lastPostsQuery = (
                    from ft in topicSet
                    join fp in postSet on ft.Id equals fp.TopicId
                    where forumIds.Contains(ft.ForumId) && ft.Published && fp.Published
                    orderby fp.CreatedOnUtc descending, ft.CreatedOnUtc descending
                    select ft.ForumId)
                    .Distinct()
                    .SelectMany(key => postSet
                        .Include(x => x.ForumTopic)
                        .ThenInclude(x => x.Forum)
                        .Where(x => x.ForumTopic.ForumId == key)
                        .OrderByDescending(x => x.CreatedOnUtc)
                        .Take(1));

                var lastPosts = await lastPostsQuery.ToListAsync(cancelToken);
                if (lastPosts.Any())
                {
                    var numTopicsByForum = await topicSet.GetForumTopicCountsByForumIdsAsync(forumIds, cancelToken);
                    var numPostsByForum = await postSet.GetForumPostCountsByForumIdsAsync(forumIds, cancelToken);

                    foreach (var lastPost in lastPosts)
                    {
                        var forum = lastPost.ForumTopic.Forum;

                        forum.LastTopicId = lastPost.TopicId;
                        forum.LastPostId = lastPost.Id;
                        forum.LastPostCustomerId = lastPost.CustomerId;
                        forum.LastPostTime = lastPost.CreatedOnUtc;

                        forum.NumTopics = numTopicsByForum.TryGetValue(forum.Id, out var numTopics)
                            ? numTopics
                            : 0;

                        forum.NumPosts = numPostsByForum.TryGetValue(forum.Id, out var numPosts)
                            ? numPosts
                            : 0;
                    }

                    return lastPosts.Count;
                }
            }

            return 0;
        }
    }
}
