namespace Smartstore.Core.Security
{
    public static partial class AclRecordQueryExtensions
    {
        /// <summary>
        /// Applies a filter for identifier and name of the entity.
        /// </summary>
        /// <param name="query">ACL record query.</param>
        /// <param name="entity">Entity.</param>
        /// <returns>ACL record query.</returns>
        public static IQueryable<AclRecord> ApplyEntityFilter<T>(this IQueryable<AclRecord> query, T entity)
            where T : BaseEntity, IAclRestricted
        {
            Guard.NotNull(entity, nameof(entity));

            return ApplyEntityFilter(query, entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Applies a filter for identifier and name of the entity.
        /// </summary>
        /// <param name="query">ACL record query.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="entityId">Entity identifier.</param>
        /// <returns>ACL record query.</returns>
        public static IQueryable<AclRecord> ApplyEntityFilter(this IQueryable<AclRecord> query, string entityName, int entityId)
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
    }
}
