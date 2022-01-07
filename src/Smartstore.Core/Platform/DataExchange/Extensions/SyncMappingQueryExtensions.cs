namespace Smartstore.Core.DataExchange
{
    public static partial class SyncMappingQueryExtensions
    {
        /// <summary>
        /// Applies a filter to get sync mappings by entity identifiers.
        /// </summary>
        /// <param name="query">Sync mapping query.</param>
        /// <param name="entityIds">Filter by entity identifiers. <c>null</c> to ignore.</param>
        /// <param name="entityName">Filter by entity name. <c>null</c> to ignore.</param>
        /// <param name="contextName">Filter by context name. <c>null</c> to ignore.</param>
        /// <returns>Sync mapping query.</returns>
        public static IQueryable<SyncMapping> ApplyEntityFilter(
            this IQueryable<SyncMapping> query,
            int[] entityIds = null,
            string entityName = null,
            string contextName = null)
        {
            Guard.NotNull(query, nameof(query));

            if (entityIds?.Any() ?? false)
            {
                query = query.Where(x => entityIds.Contains(x.EntityId));
            }

            if (entityName.HasValue())
            {
                query = query.Where(x => x.EntityName == entityName);
            }

            if (contextName.HasValue())
            {
                query = query.Where(x => x.ContextName == contextName);
            }

            return query;
        }
    }
}
