using System.Linq;
using Smartstore.Core.Identity;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    internal static partial class ForumTopicQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="ForumTopic.TopicTypeId"/> descending, then by <see cref="ForumTopic.LastPostTime"/> descending,
        /// then by <see cref="ForumTopic.Id"/> descending.
        /// </summary>
        /// <param name="query">Forum topic query.</param>
        /// <param name="customer">Customer, usually the current customer.</param>
        /// <param name="includeHidden">Applies filter by <see cref="ForumTopic.Published"/> if <paramref name="customer"/> is not a forum moderator.</param>
        /// <returns>Forum topic query.</returns>
        internal static IQueryable<ForumTopic> ApplyStandardFilter(
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
    }
}
