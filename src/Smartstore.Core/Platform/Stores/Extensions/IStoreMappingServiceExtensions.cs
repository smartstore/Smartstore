using System;
using System.Threading.Tasks;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore
{
    public static class IStoreMappingServiceExtensions
    {
        /// <summary>
        /// Finds store identifiers with granted access (mapped to the entity)
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>Store identifiers</returns>
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
        /// <returns>true - authorized; otherwise, false</returns>
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
        /// <param name="entity">Entity to check</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public static Task<bool> AuthorizeAsync<T>(this IStoreMappingService svc, T entity, int storeId) where T : BaseEntity, IStoreRestricted
        {
            if (entity == null)
                return Task.FromResult(false);

            if (!entity.LimitedToStores)
                return Task.FromResult(true);

            return svc.AuthorizeAsync(entity.GetEntityName(), entity.Id, storeId);
        }
    }
}