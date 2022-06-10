namespace Smartstore.Core.Stores
{
    public static partial class StoreMappingQueryExtensions
    {
        /// <summary>
        /// Applies a filter for identifier and name of the entity.
        /// </summary>
        /// <param name="query">Store mapping query.</param>
        /// <param name="entity"></param>
        /// <returns>Store mapping query.</returns>
        public static IQueryable<StoreMapping> ApplyEntityFilter<T>(this IQueryable<StoreMapping> query, T entity)
            where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(entity, nameof(entity));

            return ApplyEntityFilter(query, entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Applies a filter for identifier and name of the entity.
        /// </summary>
        /// <param name="query">Store mapping query.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="entityId">Entity identifier.</param>
        /// <returns>Store mapping query.</returns>
        public static IQueryable<StoreMapping> ApplyEntityFilter(this IQueryable<StoreMapping> query, string entityName, int entityId)
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