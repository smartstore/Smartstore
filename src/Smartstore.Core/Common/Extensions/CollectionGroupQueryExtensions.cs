using Smartstore.Core.Common;

namespace Smartstore
{
    public static partial class CollectionGroupQueryExtensions
    {
        /// <summary>
        /// Applies a filter to get collection groups by entity identifiers
        /// and sorts by <see cref="CollectionGroup.DisplayOrder"/>, then by <see cref="CollectionGroup.Name"/>.
        /// </summary>
        /// <param name="entityName">Filter by entity name. <c>null</c> to ignore.</param>
        /// <param name="includeHidden">Applies filter by <see cref="CollectionGroup.Published"/>.</param>
        /// <returns>Collection group query.</returns>
        public static IOrderedQueryable<CollectionGroup> ApplyEntityFilter(this IQueryable<CollectionGroup> query,
            string entityName = null,
            bool includeHidden = false)
        {
            Guard.NotNull(query);

            if (entityName.HasValue())
            {
                query = query.Where(x => x.EntityName == entityName);
            }

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);
        }
    }
}
