using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Topics
{
    public static partial class TopicQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="Topic.Priority"/>, then by <see cref="Topic.SystemName"/>.
        /// </summary>
        /// <param name="query">Topic query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Topic.IsPublished"/>.</param>
        /// <param name="customerRoleIds">Customer roles identifiers to apply filter by ACL restriction.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>Topic query.</returns>
        public static IOrderedQueryable<Topic> ApplyStandardFilter(this IQueryable<Topic> query,
            bool includeHidden = false,
            int[] customerRoleIds = null,
            int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.IsPublished);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            if (customerRoleIds != null)
            {
                query = query.ApplyAclFilter(customerRoleIds);
            }

            return query.OrderBy(x => x.Priority).ThenBy(x => x.SystemName);
        }
    }
}
