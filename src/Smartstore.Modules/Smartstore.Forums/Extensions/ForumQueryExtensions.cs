using System.Linq;
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
    }
}
