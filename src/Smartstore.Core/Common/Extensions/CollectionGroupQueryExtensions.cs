using Smartstore.Core.Common;

namespace Smartstore
{
    public static partial class CollectionGroupQueryExtensions
    {
        /// <summary>
        /// Applies a filter to get collection groups by entity identifiers
        /// and sorts by <see cref="CollectionGroup.DisplayOrder"/>, then by <see cref="CollectionGroup.Name"/>.
        /// </summary>
        /// <param name="entityIds">Filter by entity identifiers. <c>null</c> to ignore.</param>
        /// <param name="entityName">Filter by entity name. <c>null</c> to ignore.</param>
        /// <returns>Collection group query.</returns>
        public static IOrderedQueryable<CollectionGroup> ApplyEntityFilter(this IQueryable<CollectionGroup> query,
            int[] entityIds = null,
            string entityName = null)
        {
            Guard.NotNull(query);

            if (!entityIds.IsNullOrEmpty())
            {
                query = query.Where(x => entityIds.Contains(x.EntityId));
            }

            if (entityName.HasValue())
            {
                query = query.Where(x => x.EntityName == entityName);
            }

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);
        }
    }
}
