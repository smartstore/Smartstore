namespace Smartstore.Core.Content.Media
{
    public static class DownloadQueryExtensions
    {
        /// <summary>
        /// Applies a filter for identifier and name of the entity.
        /// </summary>
        /// <param name="entity">The entity instance to get downloads for.</param>
        public static IQueryable<Download> ApplyEntityFilter<T>(this IQueryable<Download> query, T entity)
            where T : BaseEntity
        {
            Guard.NotNull(entity, nameof(entity));

            return ApplyEntityFilter(query, entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Applies a filter for identifier and name of the entity.
        /// </summary>
        /// <param name="entityName">Name of entity to get downloads for.</param>
        /// <param name="entityId">Identifier of entity to get downloads for.</param>
        public static IQueryable<Download> ApplyEntityFilter(this IQueryable<Download> query, string entityName, int entityId)
        {
            Guard.NotNull(query, nameof(query));

            if (entityId != 0)
            {
                query = query.Where(x => x.EntityId == entityId);
            }

            if (entityName.HasValue())
            {
                query = query.Where(x => x.EntityName == entityName);
            }

            return query;
        }

        /// <summary>
        /// Applies a filter for entity name and multiple identifiers.
        /// </summary>
        /// <param name="entityName">Name of entity to get downloads for.</param>
        /// <param name="entityIds">Identifiers of entities to get downloads for.</param>
        public static IQueryable<Download> ApplyEntityFilter(this IQueryable<Download> query, string entityName, int[] entityIds)
        {
            Guard.NotNull(query, nameof(query));

            if (entityIds != null && entityIds.Length > 0)
            {
                query = query.Where(x => entityIds.Contains(x.EntityId));
            }

            if (entityName.HasValue())
            {
                query = query.Where(x => x.EntityName == entityName);
            }

            return query;
        }

        /// <summary>
        /// Applies a version filter.
        /// </summary>
        /// <param name="version">
        ///     The version to filter by. 
        ///     <see cref="string.Empty"/> = Any versioned file.
        ///     Any string = All files with exact version match.
        ///     <c>null</c> = No filter.
        /// </param>
        public static IQueryable<Download> ApplyVersionFilter(this IQueryable<Download> query, string version = "")
        {
            Guard.NotNull(query, nameof(query));

            if (version == string.Empty)
            {
                query = query.Where(x => !string.IsNullOrEmpty(x.FileVersion));
            }
            else if (version.HasValue())
            {
                query = query.Where(x => x.FileVersion == version);
            }

            return query;
        }
    }
}
