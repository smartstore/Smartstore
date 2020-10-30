using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Dasync.Collections;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Checks whether an entity can be accessed in a given store.
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public static IAsyncEnumerable<T> SelectAuthorizedAsync<T>(this IStoreMappingService svc, IEnumerable<T> entities, int storeId) 
            where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(entities, nameof(entities));

            return entities.WhereAsync(x => svc.AuthorizeAsync(x.GetEntityName(), x.Id, storeId));
        }
    }
}