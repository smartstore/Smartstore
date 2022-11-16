namespace Smartstore.Core.Stores
{
    public static class IStoreMappingServiceExtensions
    {
        /// <summary>
        /// Finds store identifiers with granted access (mapped to the entity).
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity to check.</param>
        /// <returns>Store identifiers.</returns>
        public static Task<int[]> GetAuthorizedStoreIdsAsync<T>(this IStoreMappingService svc, T entity) where T : BaseEntity, IStoreRestricted
        {
            if (entity == null)
                return Task.FromResult(Array.Empty<int>());

            return svc.GetAuthorizedStoreIdsAsync(entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Checks whether an entity can be accessed in the current store.
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns><c>true</c> authorized, otherwise <c>false</c>.</returns>
        public static Task<bool> AuthorizeAsync<T>(this IStoreMappingService svc, T entity) where T : BaseEntity, IStoreRestricted
        {
            if (entity == null)
                return Task.FromResult(false);

            if (!entity.LimitedToStores)
                return Task.FromResult(true);

            return svc.AuthorizeAsync(entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Checks whether an entity can be accessed in a given store.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="svc">Store mapping service.</param>
        /// <param name="entity">Entity to check.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns><c>true</c> authorized, otherwise <c>false</c>.</returns>
        public static Task<bool> AuthorizeAsync<T>(this IStoreMappingService svc, T entity, int storeId) where T : BaseEntity, IStoreRestricted
        {
            if (entity == null)
                return Task.FromResult(false);

            if (!entity.LimitedToStores)
                return Task.FromResult(true);

            return svc.AuthorizeAsync(entity.GetEntityName(), entity.Id, storeId);
        }

        /// <summary>
        /// Checks whether entities are accessible in a given store.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">Store mapping service.</param>
        /// <param name="entities">Entities to check.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Authorized entities.</returns>
        public static IAsyncEnumerable<T> SelectAuthorizedAsync<T>(this IStoreMappingService service, IEnumerable<T> entities, int storeId)
            where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(entities, nameof(entities));

            return entities.WhereAwait(async x => await service.AuthorizeAsync(x, storeId));
        }
    }
}